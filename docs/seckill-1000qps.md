# 1000 QPS 秒杀方案（Kafka + outbox 生产链路）

> 目标：在**不超卖**的前提下，把秒杀入口做到 1000+ QPS，同时把 MySQL 从请求关键路径移走。
>
> 当前实测（Kafka + Redis List outbox）：
>
> - **1592 QPS @ 200 并发**，5000 请求全部落库，0 失败
> - **3042 QPS @ 400 并发**，10000 请求全部落库，0 失败
> - 压测结束后 `outboxLength=0`、`pending=0`、DB 库存与 Redis 分桶一致

---

## 1. 为什么原来的实现只有约 10 TPS

原来的 `DeductWithRedisAsync` 虽然是 Redis 预扣，但每个请求仍然：

```text
BEGIN
  UPDATE t1_goods_stock SET goods_stock = goods_stock - 1 WHERE id = 2 ...
  SELECT ...
  INSERT t1_inventory_order
  INSERT t1_inventory_stock_flow
COMMIT
```

问题不在 Redis，而在 MySQL：

- 所有请求更新**同一行库存**，InnoDB 行锁一直持有到 commit
- 本机 `innodb_flush_log_at_trx_commit=1`、`sync_binlog=1`，每次 commit 约 100ms
- 行锁 + commit 串行化 → `1 / 100ms ≈ 10 TPS`
- 并发度从 1 加到 16，TPS 基本不变，只是锁等待变多

实测 MySQL：

```text
Innodb_row_lock_waits     = 5171
Innodb_row_lock_time_avg  = 436ms
max_connections           = 20
```

所以 1000 QPS 的核心不是“把 Redis 换快”，而是：

> **请求路径不要碰 MySQL；把行锁竞争压缩成后台批量事务。**

---

## 2. 目标架构

```text
                    POST /api/seckill/{productId}
                              │
                              ▼
                 SeckillController
        [DisableAuditing] [UnitOfWork(Disabled)]
                              │
                              ▼
                 Redis Lua（原子）
        ┌─────────────────────┴─────────────────────┐
        │ KEYS[1]   幂等键                           │
        │ KEYS[2]   outbox（List）                   │
        │ KEYS[3]   processing（List）               │
        │ KEYS[4..11] 8 个库存桶                     │
        │ ARGV[1] 数量  ARGV[2] 起始桶               │
        │ ARGV[3..5] runId / requestId / productId   │
        └─────────────────────┬─────────────────────┘
                              │ DECRBY + LPUSH 成功
                              ▼
                 立即返回 accepted（不碰 MySQL）
                              │
                              ▼
                 SeckillOutboxRelay
        RPOPLPUSH outbox -> processing
                 │
                 ▼
        Kafka Producer（acks=all, idempotence）
                 │ 投递成功
                 ▼
        LREM processing（确认）
                 │
                 ▼
        Kafka topic: seckill-orders（8 partitions）
                 │
                 ▼
                 SeckillKafkaConsumer
        手动提交 offset，最多 500 条/批
                 │
                 ▼
                 SeckillBatchWriter
        MySQL 单事务批量落库
        UPDATE stock - N
        INSERT N 条订单
        INSERT N 条库存流水
        COMMIT
                 │
                 ▼
        Kafka Commit(offset + 1)
```

### 请求路径

```text
HTTP → Redis Lua（DECRBY + LPUSH）→ 立即返回 accepted
```

不查库、不写库、不发布 ABP 审计事件、不创建 UoW。

### 后台路径

```text
Redis List outbox → Kafka → 批量消费者 → 一个事务最多 500 条
```

把 1000 次行锁竞争压缩成约 2~5 次批量事务/秒。

---

## 3. 关键设计

### 3.1 Redis 分桶

库存按 8 个桶拆开：

```text
seckill:{runId}:stock:{productId}:0
seckill:{runId}:stock:{productId}:1
...
seckill:{runId}:stock:{productId}:7
```

请求用 `requestId` 的 FNV-1a 哈希决定起始桶，Lua 从该桶开始环形扫描，桶空则尝试下一个桶。

这样：

- 请求均匀落到 8 个桶，不会总打 1 号桶
- 单桶 Lua 原子扣减
- 桶足够多时，单 key 热点被摊开

### 3.2 Lua：幂等 + 扣减 + 写 outbox

```lua
if redis.call('EXISTS', KEYS[1]) == 1 then
    return -3          -- 重复请求
end

local backlog = redis.call('LLEN', KEYS[2]) + redis.call('LLEN', KEYS[3])
if backlog >= tonumber(ARGV[6]) then
    return -4          -- outbox 积压，快速失败
end

local qty = tonumber(ARGV[1])
local start = tonumber(ARGV[2]) or 0
local bucketCount = #KEYS - 3

for offset = 0, bucketCount - 1 do
    local slot = (start + offset) % bucketCount
    local i = 4 + slot
    local stock = tonumber(redis.call('GET', KEYS[i]))
    if stock and stock >= qty then
        redis.call('DECRBY', KEYS[i], qty)
        redis.call('SET', KEYS[1], '1', 'EX', 3600)

        local payload = '{"RunId":' .. ARGV[3]
            .. ',"ProductId":' .. ARGV[5]
            .. ',"RequestId":"' .. ARGV[4] .. '"'
            .. ',"Quantity":' .. ARGV[1]
            .. ',"Bucket":' .. slot .. '}'

        redis.call('LPUSH', KEYS[2], payload)
        return slot + 1
    end
end

return 0               -- 库存不足
```

- 幂等、扣减、写 outbox 在同一个 Lua 里，天然原子
- `requestId` 已过滤为字母/数字/`-`/`_`，不会破坏 JSON
- 成功返回桶号，失败返回 `0` / `-3` / `-4`
- 不需要先查再扣，避免竞态

### 3.3 为什么用 Redis List，而不是 Redis Stream

现场 Redis 实例不支持 `XADD` / `XREAD`（运行时返回 `ERR unknown command 'XREAD'`），因此 outbox 使用最基础的 List：

```text
LPUSH seckill:outbox <payload>
RPOPLPUSH seckill:outbox seckill:outbox:processing
LREM seckill:outbox:processing 1 <payload>
```

- `LPUSH + RPOPLPUSH` 组成可靠 FIFO
- 投递失败时消息仍在 `processing`，不会丢
- 进程崩溃后，启动时把 `processing` 全部 `RPOPLPUSH` 回 `outbox` 重投
- 允许重复投递，消费者按 `order_no` 幂等

> 如果环境支持 Redis Stream，也可以换成 `XADD` + `XREADGROUP` + `XACK`；
> 当前实现刻意选择 List，保证老版本 Redis 也能跑。

### 3.4 为什么 outbox 不放在 MySQL

如果把 outbox 表放在 MySQL，请求路径仍然要：

```text
BEGIN → INSERT outbox → COMMIT
```

又会回到每请求一次 fsync（约 100ms），1000 QPS 直接退化成约 10 TPS。

因此本方案的取舍是：

- **库存权威**在 Redis（Lua 原子扣减）
- **outbox** 在 Redis List（请求路径只多一次 `LPUSH`）
- **持久化传输**交给 Kafka（`acks=all` + producer idempotence）
- **最终落库**由 Kafka 消费者批量完成

生产环境必须给 Redis 开启 AOF/RDB 持久化；否则 Redis 宕机时，
“已 accepted 但还没投递 Kafka”的短窗口消息仍可能丢失。

### 3.5 背压

Lua 每次计算：

```lua
local backlog = LLEN(outbox) + LLEN(processing)
if backlog >= 100000 then return -4 end
```

- outbox/processing 积压超过 10 万条时返回 `busy`
- 避免 Kafka 长时间不可用时 Redis 内存无限增长
- 秒杀库存本身有限，天然限制总 accepted 数量

### 3.6 后台批量落库

`SeckillBatchWriter`：

- 一个事务内：
  - 查询本批 `order_no` 是否已存在
  - 只对**新增**消息聚合扣减 `t1_goods_stock`
  - `INSERT` 新增的 `t1_inventory_order`
  - `INSERT` 新增的 `t1_inventory_stock_flow`
- 库存行锁只在一批内持有一次，而不是每个请求一次
- 订单号 `SK-{runId}-{requestId}`，`sale_no` 同订单号；`runId` 使用完整 Ticks，避免活动间碰撞
- 流水 `source = seckill`，可在审计页按来源筛选

### 3.7 至少一次投递与幂等

Kafka 链路是 at-least-once，以下场景都会产生重复消息：

- relay 投递成功，但删除 `processing` 前崩溃
- 消费者落库成功，但提交 offset 前崩溃
- Kafka rebalance / 重试

消费者依靠三层幂等保证不重复扣库存：

1. `SeckillBatchWriter` 按批次内 `order_no` 去重
2. 落库前查询已存在的 `order_no`，只处理新增消息
3. `t1_inventory_order.order_no` 唯一索引兜底

### 3.8 失败处理

消费者对每批最多重试 3 次：

- 成功：写状态 `created` → 提交 offset
- 连续失败：写状态 `failed` → 提交 offset，避免毒消息阻塞分区

生产环境还应增加：

- 死信队列（DLQ）
- 对账任务：扫描 `failed` 请求，确认 DB 是否已落单，未落单则回补 Redis 分桶
- 告警：`failed > 0`、`outboxLength` 持续增长、消费延迟

### 3.9 状态机

```text
accepted  → Lua 成功，已写入 outbox
created   → Kafka 消费者批量落库成功
failed    → 连续落库失败
soldout   → Redis 库存不足
busy      → outbox 积压过大
duplicate → 幂等命中，返回已有状态
```

状态写在 Redis：

```text
seckill:{runId}:status:{requestId}
```

查询接口：

```http
GET /api/seckill/status/{requestId}
```

### 3.10 重置与运行隔离

每次重置生成新的 `runId`：

- 新库存桶使用新前缀
- 旧请求的状态/幂等键自然过期
- 重置会等待 `pending=0` 且 `outbox + processing` 清空
- 清除历史 `SK-` 订单/流水，把 DB 库存设为初始值

```http
POST /api/seckill/reset?productId=2&stock=10000
```

### 3.11 关闭审计与 UoW

秒杀 Controller：

```csharp
[ApiController]
[Route("api/seckill")]
[DisableAuditing]
[UnitOfWork(IsDisabled = true)]
public class SeckillController : ControllerBase
```

秒杀请求不写审计日志、不创建 ABP UoW，避免每请求的额外开销。

---

## 4. 接口

| 方法 | 路径 | 说明 |
| --- | --- | --- |
| POST | `/api/seckill/{productId}?quantity=1&requestId=xxx` | 秒杀入口 |
| GET | `/api/seckill/status/{requestId}` | 查询 accepted/created/failed |
| GET | `/api/seckill/stats?productId=2` | 桶库存、accepted/created/failed/pending/outboxLength |
| POST | `/api/seckill/reset?productId=2&stock=10000` | 重置秒杀库存与分桶 |

普通库存/商店接口保持不变。

---

## 5. 配置

Kafka 沿用主机的 `Kafka:Connections:Default:BootstrapServers`：

```json
{
  "Kafka": {
    "Connections": {
      "Default": {
        "BootstrapServers": "192.168.3.102:9192"
      }
    }
  },
  "Seckill": {
    "Kafka": {
      "Topic": "seckill-orders",
      "GroupId": "seckill-order-worker",
      "Partitions": 8,
      "ReplicationFactor": 1
    }
  }
}
```

`Seckill` 节点可以省略，代码有默认值：

- Topic：`seckill-orders`
- GroupId：`seckill-order-worker`
- Partitions：`8`
- ReplicationFactor：`1`（本地单机；生产改为 `3`）

消费者启动时会用 AdminClient 创建 topic（已存在则忽略）。

---

## 6. 压测

启动后端：

```powershell
dotnet run --project t1_frame/src/webapi/t1_frame.webapi.abp/t1_frame.webapi.abp.csproj --launch-profile http
```

运行 Node 压测脚本：

```powershell
# 5000 请求 / 200 并发
node t1_frame/test/seckill-loadtest.mjs http://localhost:5288 5000 200 2

# 10000 请求 / 400 并发
node t1_frame/test/seckill-loadtest.mjs http://localhost:5288 10000 400 2
```

脚本会自动：

1. 调 reset 准备库存（`total + 1000`）
2. 用 keep-alive 连接池并发发送请求
3. 统计 accepted / rejected / errors / QPS
4. 轮询 `/stats`，等待 `created + failed >= accepted`

### 实测结果（Kafka + Redis List outbox）

```text
5000 请求 / 200 并发
accepted=5000, created=5000, failed=0, pending=0
elapsed=3.14s, qps=1591.9
outboxLength=0
剩余库存=1000

10000 请求 / 400 并发
accepted=10000, created=10000, failed=0, pending=0
elapsed=3.29s, qps=3041.7
outboxLength=0
剩余库存=1000
```

两次都**没有超卖、没有失败、没有 outbox 积压**。
压测结束后查询 DB：订单 10000 条，商品库存 1000，与 Redis 一致。

---

## 7. 与旧方案的对比

| 指标 | 旧同步事务方案 | 旧内存队列方案 | 当前 Kafka + outbox |
| --- | ---: | ---: | ---: |
| 入口 QPS | ~10 | 1600~3000 | **1600~3000** |
| 请求是否访问 MySQL | 是 | 否 | **否** |
| 队列持久化 | — | 进程内存，崩溃丢失 | **Redis List + Kafka** |
| 行锁竞争 | 每请求一次 | 每批一次 | **每批一次** |
| 事务提交 | 每请求一次 fsync | 每批一次 | **每批一次** |
| 超卖 | 条件更新防住 | Redis Lua 防住 | **Redis Lua 防住** |
| 重复消息 | — | 不涉及 | **order_no 幂等** |
| 失败回补 | 事务回滚 | Redis 回补 | **failed 状态 + 对账/补偿** |
| 实时性 | 强一致 | 最终一致（秒级） | **最终一致（秒级）** |

---

## 8. 生产化 Checklist

1. **Redis 持久化**
   - 开启 AOF（推荐 `appendfsync everysec`）或至少 RDB
   - 监控 `outbox` / `processing` 长度、Redis 内存、evicted keys

2. **Kafka**
   - 生产建议 `replication-factor >= 3`、`min.insync.replicas >= 2`
   - 消费者按分区扩展；当前单实例批量消费
   - 监控 consumer lag、生产失败率、重试次数

3. **死信与对账**
   - `failed` 批次进入 DLQ
   - 定时任务核对 `Redis 分桶 + DB 订单 + DB 库存`
   - 对确认未落单的消息回补 Redis 库存

4. **运行隔离**
   - 演示环境 reset 会清理所有 `SK-` 订单
   - 生产按活动/runId 隔离订单，避免旧 Kafka 消息污染新活动
   - 消费端可按 `RunId` 过滤已结束活动

5. **Redis 集群**
   - 当前 8 个桶 + outbox/processing 在同一个 Redis 实例
   - 集群模式下多 key Lua 要求同 slot，建议 key 带 hash tag，例如：
     `seckill:{productId}:{runId}:stock:{bucket}`
   - 或每个桶单独一个 key + 单 key 脚本，客户端做 fallback

6. **数据库扩展**
   - 批量写入 500~1000 条/事务，按磁盘和 `max_connections` 调整
   - 订单、流水可按业务分库分表
   - 本机 `max_connections=20`，生产要单独评估

7. **限流与防刷**
   - 网关令牌桶、IP/用户维度限流
   - 验证码、黑名单、同用户限购
   - outbox 积压时快速失败，避免拖垮服务

8. **可观测性**
   - `accepted`、`created`、`failed`、`pending`、`outboxLength`
   - 各桶剩余库存
   - 落库延迟（accepted → created）
   - MySQL 批量事务耗时、Kafka lag、Redis 错误率

---

## 9. 结论

> **1000 QPS 的关键不是 Redis，而是把 MySQL 事务移出请求路径。**
>
> 当前方案用 Redis Lua 做唯一库存闸门，同一个脚本写入 Redis List outbox；
> relay 以 `RPOPLPUSH + acks=all` 投递 Kafka，消费者批量落库并手动提交 offset。
> 实测 1600~3000 QPS，10000 单全部落库、0 失败、0 超卖、0 outbox 积压。
>
> 上生产前还需要补齐：Redis AOF、Kafka 多副本、DLQ/对账、活动隔离、限流和监控。

---

## 10. 代码位置

```text
t1_frame/src/application/t1_frame.application.abp/Seckill/
├── SeckillService.cs          # Redis Lua 闸门、List outbox、状态、统计、重置
├── SeckillOutboxRelay.cs      # List outbox -> Kafka 投递器
├── SeckillKafkaConsumer.cs    # Kafka 批量消费者、手动提交 offset
├── SeckillBatchWriter.cs      # 幂等批量落库
├── SeckillQueue.cs            # 进程内指标计数器
├── SeckillKafkaOptions.cs     # topic / group / partitions
├── SeckillOrderMessage.cs
└── SeckillDtos.cs

t1_frame/src/webapi/t1_frame.webapi.abp/
├── HostWebHostModule.cs       # 注册 relay / consumer / options
└── Controllers/SeckillController.cs

t1_frame/test/
└── seckill-loadtest.mjs       # Node 压测脚本
```

启动/压测方式见本文第 6 节。
