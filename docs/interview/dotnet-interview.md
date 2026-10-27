# .NET 经典面试题（平台与框架）

> 面向 ASP.NET Core 后端岗位：请求管道、依赖注入、EF Core、分布式设计与电商场景题。
> 每题给出**要点答案**与**常见追问**；建议先遮住答案自测，标记答不出的题。
> 语言基础、GC、集合/LINQ、并发异步见 [csharp-interview.md](csharp-interview.md)。

## 目录

- [一、ASP.NET Core](#一aspnet-core)
- [二、EF Core](#二ef-core)
- [三、分布式与架构](#三分布式与架构)
- [四、经典业务场景题（电商）](#四经典业务场景题电商)
- [五、自测清单](#五自测清单)
- [附录 A：15 分钟背诵卡](#附录-a15-分钟背诵卡)
- [附录 B：模拟面试 20 题（含追问链）](#附录-b模拟面试-20-题含追问链)
- [附录 C：口述版——3 分钟讲清一次请求的旅程](#附录-c口述版3-分钟讲清一次请求的旅程)

---

## 一、ASP.NET Core

### 1. 请求管道和中间件？

中间件按注册顺序组成管道，形成“洋葱模型”：`next()` 之前是请求处理，之后是响应处理。

典型顺序：

```
异常处理 → HSTS/HTTPS 重定向 → 静态文件 → 路由 → CORS → 认证 → 授权 → 端点
```

> 追问：`Use`（可调 next）、`Run`（终端，不调 next）、`Map` / `MapWhen`（分支）。

---

### 2. DI 的三种生命周期？

| 生命周期 | 说明 | 典型用途 |
| --- | --- | --- |
| `Singleton` | 全局一份 | 配置、缓存、`HttpClientFactory` |
| `Scoped` | 每请求一份 | `DbContext`、工作单元、当前用户 |
| `Transient` | 每次解析新建 | 轻量无状态服务 |

**坑**：
- Singleton 注入 Scoped → 捕获依赖（`DbContext` 被长期持有，且非线程安全）。
- 中间件构造函数注入 Scoped 会退化为 Singleton，应改用 `InvokeAsync` 方法参数注入。

```csharp
app.Use(async (ctx, next) =>
{
    var repo = ctx.RequestServices.GetRequiredService<IUserRepository>(); // 每请求解析
    await next();
});
```

---

### 3. 过滤器有哪些？执行顺序？

授权 → 资源 → 动作 → 异常 → 结果。

- `IAuthorizationFilter`
- `IResourceFilter`（可短路，缓存常用）
- `IActionFilter` / `IAsyncActionFilter`
- `IExceptionFilter`
- `IResultFilter`

---

### 4. 认证和授权怎么做的？

- 认证（你是谁）：JWT Bearer、Cookie、OIDC。
- 授权（你能做什么）：`[Authorize]`、策略 `AddPolicy`、基于资源/声明的授权、自定义 `IAuthorizationHandler`。

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { /* TokenValidationParameters */ });

builder.Services.AddAuthorization(o =>
    o.AddPolicy("AdminOnly", p => p.RequireRole("Admin")));
```

---

### 5. 配置系统的优先级？

`appsettings.json` → `appsettings.{Environment}.json` → 用户机密（开发）→ 环境变量 → 命令行参数。**后加载覆盖先加载**。

---

### 6. 如何提升 API 性能？

- 全链路异步，避免同步阻塞。
- EF Core 只读查询 `AsNoTracking()`，分页，避免 N+1。
- 多级缓存（内存 + Redis）、响应缓存、输出压缩。
- 连接池（`HttpClientFactory`、`DbContextPool`）。
- 流式处理大响应（`IAsyncEnumerable` + `Response.BodyWriter`）。
- 限流、熔断、降级。

---

### 7. `IHttpContextAccessor` 为什么默认不注册？

`HttpContext` 不是线程安全的，且后台任务中不存在请求上下文。需要时显式 `AddHttpContextAccessor()`，并在后台任务中新建 scope。优先通过方法参数传递所需数据，而不是到处取 `HttpContext`。

---

## 二、EF Core

> 这里只保留 5 道速览题；**完整版（30 题：跟踪原理、映射、N+1、并发、迁移、拦截器）见 [efcore-interview.md](efcore-interview.md)**。

### 8. `DbContext` 的生命周期？线程安全吗？

Scoped（每请求一个）。**非线程安全**，不能并行使用同一实例；后台任务要新建 scope。

```csharp
using var scope = serviceProvider.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
```

---

### 9. 跟踪查询 vs 无跟踪？

默认跟踪会生成快照用于变更检测，只读查询用 `AsNoTracking()` 提升性能、减少内存。

---

### 10. 怎么避免 N+1？

- `Include` / `ThenInclude` 预加载导航属性。
- 投影 `Select` 只取需要列。
- 集合包含多个时注意笛卡尔积膨胀，必要时 `AsSplitQuery()`。

```csharp
var users = await db.Users
    .AsNoTracking()
    .Include(u => u.Orders)
    .Where(u => u.IsActive)
    .ToListAsync();
```

---

### 11. 并发冲突怎么处理？

乐观并发：`[ConcurrencyCheck]` 或 `IsRowVersion()`（SQL Server 的 `rowversion`），更新时校验版本。

```csharp
try
{
    await db.SaveChangesAsync();
}
catch (DbUpdateConcurrencyException ex)
{
    // 重新加载、合并或提示用户
}
```

---

### 12. 迁移（Migrations）流程？

```powershell
dotnet ef migrations add AddUserTable --project src/.../xxx.entityframeworkcore
dotnet ef database update --project src/.../xxx.entityframeworkcore
```

生产环境推荐 `dotnet ef migrations script` 生成 SQL 脚本，评审后执行，避免应用启动时自动迁移。

---

## 三、分布式与架构

### 13. 缓存穿透、击穿、雪崩？

- **穿透**：查询不存在的数据，每次都打到 DB → 布隆过滤器 / 缓存空值。
- **击穿**：热点 key 过期瞬间大量请求 → 互斥锁重建 / 逻辑过期。
- **雪崩**：大量 key 同时过期或缓存宕机 → 随机过期时间、多级缓存、熔断降级。

---

### 14. 如何保证接口幂等？

- 唯一业务号 + 数据库唯一索引。
- Redis `SET key value NX PX` 去重。
- 状态机校验（只允许从“待支付”变为“已支付”）。
- 消息消费端去重表（消息 ID）。

---

### 15. 分布式锁怎么做？

Redis：`SET key val NX PX` 加锁，Lua 脚本校验持有者后释放；注意锁续期（看门狗）与误删。

```lua
-- 释放锁：只有持有者才能删
if redis.call("GET", KEYS[1]) == ARGV[1] then
    return redis.call("DEL", KEYS[1])
else
    return 0
end
```

其他方案：ZooKeeper / etcd（临时顺序节点，天然支持公平锁）。

---

### 16. CAP 与最终一致性？

分布式事务优先**避免**：本地消息表、事务消息、Saga、TCC。跨服务追求最终一致 + 补偿 + 幂等。

---

### 17. 如何设计一个高并发接口？

- 分层削峰：CDN → 网关限流 → 应用层缓存 → 队列异步化 → DB 读写分离/分库分表。
- 无状态服务 + 水平扩容。
- 热点数据隔离，避免单点。
- 降级预案与压测（明确 QPS / P99 目标）。

---

## 四、经典业务场景题（电商）

> 这一章是“八股之外的硬仗”。面试官问业务场景时，真正想听的是：**你知不知道并发下会出什么问题、方案之间怎么取舍、失败后怎么补偿**。
> 回答套路：`场景 → 问题 → 方案演进 → 取舍 → 一致性/幂等/补偿`。
>
> 配套可运行示例：`t1_frame/src/application/t1_frame.application.abp/Inventory`，
> 接入现有 MySQL + Redis + RabbitMQ/Kafka + MongoDB，启动 `t1_frame.webapi.abp` 后访问
> `/api/app/inventory/...`，提供 naive / atomic / optimistic / redis / idempotent 五种扣减方式
> 和一个并发压测接口。配套 Vue 前端在 `t1_frame/frontend/inventory-web`
> （商品商店 / 库存管理 / 订单管理 / 审计日志），支持下单、支付、库存扣减、11 套换肤与中英双语。
> 另外实现了 Redis 分桶 + 批量落库的 1000 QPS 秒杀通道，见 [../seckill-1000qps.md](../seckill-1000qps.md)。
> 运行说明与实测结果见 [../inventory-deduction.md](../inventory-deduction.md)。

### 18. 库存扣减：超卖是怎么产生的？

先看一段典型的错误代码：

```csharp
var product = await db.Products.FirstAsync(p => p.Id == id); // T1、T2 都读到 10
if (product.Stock < 1) throw new BizException("库存不足");
product.Stock -= 1;                                          // T1 写 9
await db.SaveChangesAsync();                                  // T2 也写 9 → 只扣了 1 件
```

两个请求同时“读—判断—写”，后写覆盖前写，这就是**丢失更新**。压测时表现为：库存 100，卖出 130 单。

> 根因：**检查与扣减不是一个原子操作**。所有方案的本质都是把“判断 + 扣减”合并成一个原子动作。

---

### 19. 库存扣减方案对比

| 方案 | 核心做法 | 优点 | 缺点 | 适用场景 |
| --- | --- | --- | --- | --- |
| **DB 条件更新** | `SET stock = stock - n WHERE stock >= n` | 简单、强一致、天然防超卖 | 热点行有行锁竞争 | 绝大多数业务（首选） |
| **乐观锁 version** | `WHERE version = @v` | 无长事务、读多写少友好 | 冲突需重试，高并发失败率高 | 冲突不激烈的更新 |
| **悲观锁** | `SELECT ... FOR UPDATE` | 逻辑直观 | 排队、易锁等待/死锁 | 低并发、强一致场景 |
| **Redis + Lua 预扣** | 内存原子脚本，异步落库 | 极高吞吐 | 最终一致、需补偿对账 | 秒杀、大促 |
| **MQ 串行化** | 单分区顺序消费扣减 | 削峰、天然串行 | 有延迟、吞吐受单分区限制 | 异步下单 |
| **分段库存** | 一个大库存拆成 N 个桶 | 打散热点 | 复杂度高、可能“有的桶空有的桶剩” | 超级热点商品 |
| **分布式锁** | Redis/ZK 锁住商品 | 思路简单 | 性能差、锁超时/续期问题 | **不推荐**用于扣库存 |

**面试结论**：普通业务用 **DB 条件更新 + 事务 + 幂等** 就够了，不要上来就分布式锁；秒杀才上 Redis 预扣 + MQ 削峰。

---

### 20. 方案一：数据库原子扣减（标准答案）

MySQL / InnoDB 下，`UPDATE` 会对目标行加排他锁，且 `stock >= n` 的判断在锁内完成，天然原子：

```sql
UPDATE t_product
SET stock = stock - #{quantity}, update_time = NOW()
WHERE id = #{productId}
  AND stock >= #{quantity};
-- 受影响行数 = 0 → 库存不足（或商品不存在）
```

EF Core 7+ 可以用 `ExecuteUpdateAsync`（注意：**绕过 ChangeTracker**，执行后实体缓存不会自动更新）：

```csharp
public async Task DeductStockAsync(long productId, int quantity)
{
    var affected = await _db.Products
        .Where(p => p.Id == productId && p.Stock >= quantity)
        .ExecuteUpdateAsync(setters => setters
            .SetProperty(p => p.Stock, p => p.Stock - quantity)
            .SetProperty(p => p.UpdateTime, DateTime.Now));

    if (affected == 0)
    {
        throw new BizException("库存不足");
    }
}
```

关键点（面试加分）：

1. **必须校验受影响行数**，不能只看有没有抛异常。
2. 扣库存与写订单、写库存流水放在**同一个本地事务**里，要么全成功要么全回滚。
3. 事务要短，不要在事务里调远程接口、发 MQ、做耗时计算。
4. 批量扣多个商品时**按商品 ID 排序**再加锁，避免交叉死锁。
5. `ExecuteUpdate` 后如果要继续用实体，需重新查询或 `AsNoTracking`。

---

### 21. 方案二：乐观锁

```sql
UPDATE t_product
SET stock = stock - #{quantity}, version = version + 1
WHERE id = #{productId}
  AND stock >= #{quantity}
  AND version = #{version};
```

```csharp
for (var i = 0; i < 3; i++)
{
    var product = await _db.Products.AsNoTracking().FirstAsync(p => p.Id == productId);
    var affected = await _db.Products
        .Where(p => p.Id == productId && p.Version == product.Version && p.Stock >= quantity)
        .ExecuteUpdateAsync(s => s
            .SetProperty(p => p.Stock, p => p.Stock - quantity)
            .SetProperty(p => p.Version, p => p.Version + 1));

    if (affected > 0) return;
}
throw new BizException("库存扣减失败，请重试");
```

> 追问：乐观锁和条件更新有什么区别？条件更新把“库存判断”交给 DB，不需要版本号；乐观锁还能保护“价格、状态”等其他字段的并发修改。库存场景条件更新更简洁。

---

### 22. 方案三：悲观锁

```sql
BEGIN;
SELECT stock FROM t_product WHERE id = #{id} FOR UPDATE;
-- 应用层判断 stock >= n
UPDATE t_product SET stock = stock - #{n} WHERE id = #{id};
COMMIT;
```

注意：必须走主键/索引，否则可能锁范围过大；事务内不要做 RPC；高并发会排队，QPS 上不去。

---

### 23. 方案四：Redis 预扣 + Lua（秒杀核心）

Lua 脚本在 Redis 中**原子执行**，把“读库存 + 判断 + 扣减”合成一步：

```lua
-- KEYS[1] = 库存 key，ARGV[1] = 扣减数量
local stock = tonumber(redis.call('GET', KEYS[1]))
if stock == nil then
    return -1            -- 库存未初始化
end
if stock < tonumber(ARGV[1]) then
    return 0             -- 库存不足
end
redis.call('DECRBY', KEYS[1], ARGV[1])
return 1                 -- 扣减成功
```

C# 调用：

```csharp
private const string DeductScript = @"
local stock = tonumber(redis.call('GET', KEYS[1]))
if stock == nil then return -1 end
if stock < tonumber(ARGV[1]) then return 0 end
redis.call('DECRBY', KEYS[1], ARGV[1])
return 1";

public async Task<bool> TryDeductAsync(long productId, int quantity)
{
    var result = await _redis.ScriptEvaluateAsync(
        DeductScript,
        new RedisKey[] { $"stock:{productId}" },
        new RedisValue[] { quantity });

    return (long)result == 1;
}
```

流程与一致性：

```
请求 → 校验/限购 → Redis Lua 预扣 → 发 MQ → 异步落库（DB 条件扣减）
                                      ↓ 失败
                                  回补 Redis + 告警/对账
```

- **以 DB 为准**，Redis 只是“挡流量的预扣层”。
- DB 扣减失败必须**回补 Redis**，否则少卖；定时任务用 DB 库存**校正** Redis。
- 消息要保证不丢：本地消息表 / 事务消息 / 发送确认 + 重试 + 死信队列。
- 防止重复消费：消息带业务唯一号，消费端幂等。

> 追问：Redis 挂了怎么办？降级到 DB 条件扣减 + 限流；或直接返回“活动火爆”。不能既读不到库存又放行。

---

### 24. 方案五：分段库存（超级热点）

一个商品 10 万库存，单 key 或单行必然成为瓶颈。拆成 100 个桶，每桶 1000：

```
stock:1001:0 ... stock:1001:99
```

- 请求按 `userId % 100` 路由到桶，桶空则顺延尝试其他桶。
- 优点：并发度提升 N 倍；缺点：实现复杂，要处理“总库存汇总”和“桶间不均衡”。
- 通常配合 Redis 预扣使用，DB 侧仍按总量做最终校验。

---

### 25. 完整下单链路（可以直接背）

```
1. 参数校验：商品状态、限购、活动时间、风控
2. 幂等校验：请求 token / 业务唯一号（Redis SETNX）
3. 预扣库存：Redis Lua（秒杀）或 DB 条件更新（普通）
4. 创建订单：待支付状态，写订单 + 订单明细 + 库存流水（本地事务）
5. 发起支付：调用支付网关，返回支付参数
6. 支付回调：验签 → 幂等 → CAS 更新订单状态 → 扣减/确认库存 → 发履约消息
7. 超时未支付：延迟消息 → 关单 → 回补库存（Redis + DB）→ 释放优惠券
8. 对账补偿：定时比对订单/库存/支付流水，修复不一致
```

**状态机**（用 CAS 更新，避免并发覆盖）：

```sql
UPDATE t_order
SET status = 2, pay_time = NOW()
WHERE id = #{orderId} AND status = 1;   -- 只有“待支付”能变成“已支付”
```

---

### 26. 库存回补与少卖

| 问题 | 原因 | 解决 |
| --- | --- | --- |
| **超卖** | 扣减非原子、失败仍下单 | 条件更新校验行数；扣减与订单同事务 |
| **少卖** | 扣了库存但订单创建失败/超时未支付 | 事务回滚；延迟消息回补；定时对账 |
| **重复扣减** | 用户重复提交、MQ 重投 | 幂等 token、唯一索引、消费去重表 |
| **回补过头** | 重复回补、关单与支付并发 | 状态机 CAS（只有“已关闭”才回补），回补流水唯一 |

回补同样要幂等：

```sql
-- 先抢占“回补资格”，再回补，避免重复归还
UPDATE t_order SET status = 5 WHERE id = #{id} AND status = 1;
-- affected = 1 才执行 stock = stock + n
```

---

### 27. 幂等设计的四种武器

1. **数据库唯一索引**：`t_order(order_no)`、`t_stock_flow(biz_no)`，重复插入直接失败。
2. **Redis SETNX**：请求 token 作为 key，设置过期时间；注意“先占位后处理，失败要删除”。
3. **状态机 CAS**：`WHERE status = 期望值`，天然幂等。
4. **去重表**：消费消息前插入 `message_id`，唯一冲突则跳过。

```csharp
var ok = await _redis.StringSetAsync($"idem:{requestId}", 1,
    TimeSpan.FromMinutes(5), When.NotExists);
if (!ok) throw new BizException("请勿重复提交");
```

---

### 28. 缓存与数据库一致性

- 常用：**先更新 DB，再删除缓存**；读时回填。
- 延迟双删：更新后删一次，延迟几百毫秒再删一次，降低并发脏读窗口。
- 更强：订阅 binlog（Canal / Debezium）异步删缓存。
- 库存场景：**DB 是唯一真相**，缓存只做加速，允许短暂不一致，靠对账收敛。

> 不要用“先删缓存再更新 DB”，并发读会把旧值回填；也不要给缓存设永不过期还指望强一致。

---

### 29. 优惠券领取（超发问题）

和库存同构，但多了“每人限领”：

```lua
-- KEYS[1]=券库存, KEYS[2]=用户已领集合, ARGV[1]=userId
if redis.call('SISMEMBER', KEYS[2], ARGV[1]) == 1 then return -2 end -- 已领
local stock = tonumber(redis.call('GET', KEYS[1]))
if stock == nil or stock <= 0 then return 0 end
redis.call('DECRBY', KEYS[1], 1)
redis.call('SADD', KEYS[2], ARGV[1])
return 1
```

DB 兜底：`UNIQUE KEY (coupon_id, user_id)`，异步发券失败要回补库存并重试。

---

### 30. 账户余额扣减

```sql
UPDATE t_account
SET balance = balance - #{amount}, version = version + 1
WHERE id = #{id} AND balance >= #{amount};
```

- 必须同时写**流水表**，业务号唯一，防重复扣款。
- 禁止“查余额 → 内存相减 → 更新”，这是典型的丢失更新。
- 金额用 `decimal`，不要用 `double`。
- 跨账户转账要按账户 ID 排序加锁，或使用 TCC/Saga。

---

### 31. 订单超时关闭怎么做？

| 方案 | 原理 | 优缺点 |
| --- | --- | --- |
| 定时扫表 | 每分钟扫“待支付且超时” | 简单，但有延迟、大表压力大 |
| RabbitMQ TTL + 死信 | 消息过期进死信队列 | 精度受队列头阻塞影响 |
| RocketMQ 延迟消息 | 指定延迟级别 | 精度较好，依赖中间件 |
| Redis ZSet | score = 到期时间，轮询 | 轻量，需自己保证可靠 |
| 时间轮 | 内存定时器 | 性能好，重启需持久化 |

无论哪种，**关单和回补都要幂等**，并且要防止“关单”和“支付成功”并发——用状态机 CAS 决出胜负。

---

### 32. 秒杀系统分层设计

```
CDN / 静态页
  → 网关限流（令牌桶、IP/用户维度）
  → 活动校验、验证码、黑名单
  → Redis 预扣（Lua）+ 分段库存
  → MQ 削峰（顺序/分区）
  → 订单服务异步落库（DB 条件扣减）
  → 支付 / 履约
```

要点：热点隔离（独立集群）、读多写少静态化、限流兜底、降级开关、压测验证（关注 P99 和 DB 连接池）。

---

### 33. 高频追问清单

- **为什么不用分布式锁扣库存？** 性能差、锁超时与续期复杂、Redis 主从切换可能丢锁；DB 原子更新本身就能保证不超卖。
- **Redis 扣成功、DB 扣失败怎么办？** 回补 Redis + 重试 + 对账；接受最终一致，不能接受“扣了钱没货”。
- **MQ 消息丢了怎么办？** 本地消息表（业务与消息同事务）+ 发送确认 + 定时补偿 + 死信人工处理。
- **如何保证消息不被重复消费？** 业务唯一号 + 消费去重表/Redis + 幂等状态机。
- **大促前怎么准备？** 容量评估、热点打散、缓存预热、限流阈值、降级预案、全链路压测、监控告警。
- **怎么证明没有超卖？** 对账 SQL：`初始库存 = 剩余库存 + 已支付数量 + 锁定数量`，定时校验并告警。

---

## 五、自测清单

打勾表示能**不看资料讲清原理并写出示例**：

- [ ] 中间件顺序与短路
- [ ] DI 三种生命周期、单例注入 Scoped 的后果
- [ ] 过滤器种类与执行顺序
- [ ] 认证 vs 授权、JWT 与刷新令牌
- [ ] 配置优先级与热更新
- [ ] EF Core 跟踪、N+1、并发冲突
- [ ] 迁移在生产环境怎么执行
- [ ] 缓存三兄弟、幂等、分布式锁
- [ ] 库存扣减的 5 种方案及取舍
- [ ] 超卖 / 少卖 / 重复扣减的成因与修复
- [ ] 下单链路、订单状态机、超时关单
- [ ] Redis 预扣的补偿与对账设计
- [ ] 秒杀分层设计（接入层 → 应用层 → 存储层）

---

> 复习建议：先过「自测清单」，答不出的回到对应章节；面试前一天重点看**DI 生命周期**、**EF 跟踪与并发**、**库存扣减方案取舍**——这三块追问最多。

---

# 附录 A：15 分钟背诵卡

> 每条一句话，细节回正文对应题号。

### ASP.NET Core（1~9 题）

1. 请求管道是一条 `RequestDelegate` 链，`Use` 可短路、`Run` 终止、`Map` 分支；**顺序即行为**。
2. 推荐顺序：异常处理 → HSTS → HTTPS 重定向 → 静态文件 → 路由 → CORS → 认证 → 授权 → 端点。
3. DI 三种生命周期：Singleton（全局一个）、Scoped（每请求一个）、Transient（每次解析新建）。
4. **单例不能依赖 Scoped**，否则 Scoped 被提升成单例（`ValidateScopes` 在开发环境会直接抛）。
5. 过滤器顺序：授权 → 资源 → 模型绑定 → 操作 → 异常 → 结果；用 `IOrderedFilter` 微调。
6. 认证回答「你是谁」（JWT/Cookie），授权回答「你能干什么」（策略/角色/声明）。
7. 配置优先级（后者覆盖前者）：`appsettings.json` → `appsettings.{Env}.json` → 用户机密 → 环境变量 → 命令行。
8. `IHttpContextAccessor` 默认不注册：它靠 `AsyncLocal`，有开销且容易被误用（如后台线程里拿不到）。
9. 提升 API 性能：全异步、`AsNoTracking`、输出缓存、响应压缩、分页、避免同步 IO 与过度序列化。

### EF Core（10~14 题）

10. `DbContext` 是 **Scoped 且非线程安全**，不能并发共享；`AddDbContextPool` 可复用实例。
11. 跟踪查询会保留快照做变更检测，只读用 `AsNoTracking()`；但需要标识解析（同一实体同一实例）时不能关。
12. N+1 的解法：`Include` 预加载、投影成 DTO、`AsSplitQuery` 拆分笛卡尔积；用日志/拦截器/MiniProfiler 发现。
13. 并发冲突：加 `[Timestamp]` / `IsRowVersion()`，捕获 `DbUpdateConcurrencyException` 后重载重试或按业务合并。
14. 生产迁移用 `dotnet ef migrations bundle` 或幂等 SQL 脚本；**别让多个实例在启动时同时 `Migrate()`**。

### 分布式与场景（15~24 题）

15. 幂等四件套：唯一索引、去重表/Redis SETNX、状态机、upsert。
16. 分布式锁：`SET NX PX` + 唯一 value + Lua 释放 + 看门狗续期。
17. CAP：一致性/可用性/分区容忍三者不可兼得；工程上选 **AP + 最终一致 + 补偿 + 对账**。
18. 库存扣减五方案：DB 原子扣减、乐观锁、悲观锁、Redis 预扣 + Lua、分段库存。
19. 防超卖最终兜底是 **`UPDATE ... WHERE stock >= n`** 与**唯一索引**，缓存只是挡流量。
20. 超卖成因：并发读改写、缓存与 DB 不一致；少卖成因：预扣成功但落库失败 → 补偿 + 对账。
21. 秒杀分层：接入层限流/CDN → 应用层无状态 + Redis 预扣 → 存储层异步批量落库。
22. 订单超时三种实现：定时扫表（简单但有延迟）、延迟队列（TTL+DLX / 插件）、Redis ZSet 轮询。
23. 优惠券超发：预扣 + 唯一索引（用户 + 券）+ 限流三件套。
24. 高并发接口的通用顺序：缓存 → 异步 → 限流 → 降级 → 池化。

> 记忆锚点：**管道 → DI → 数据层 → 分布式 → 电商场景**。

---

# 附录 B：模拟面试 20 题（含追问链）

### 1. 中间件顺序为什么重要？

**开口**：管道是洋葱模型，谁在前面谁先拿到请求、最后拿到响应；顺序错会导致认证前就短路、CORS 头缺失等。
**追问**：`UseRouting` 和 `UseEndpoints` 之间能插什么？→ 授权（`UseAuthorization`）必须在两者之间，否则拿不到端点元数据。

### 2. DI 三种生命周期怎么选？

**开口**：无状态且线程安全用 Singleton，有请求级状态用 Scoped（如 DbContext），轻量短命用 Transient。
**追问**：单例里注入 Scoped 会怎样？→ 捕获根作用域，等于把它变成单例，且可能跨请求共享状态。

### 3. 过滤器有哪几种，顺序如何？

**开口**：授权 → 资源 → 模型绑定 → 操作 → 异常 → 结果。
**追问**：想统一包响应体用哪个？→ 结果过滤器；想统一异常用异常过滤器或中间件（中间件更外层）。

### 4. JWT 怎么做登出？

**开口**：JWT 无状态不能主动失效，靠短有效期 + 刷新令牌轮换，必要时加黑名单（jti 存 Redis）。
**追问**：刷新令牌被盗怎么办？→ 一次性轮换 + 检测重放（旧 refresh 再次使用就吊销整条链）。

### 5. 配置热更新怎么实现？

**开口**：`IOptionsMonitor<T>` 监听变更，`IOptionsSnapshot<T>` 每请求取最新，`IOptions<T>` 是单例不刷新。
**追问**：改了配置要重启吗？→ 文件源默认 `reloadOnChange`，容器环境变量改了不会自动生效。

### 6. 为什么 `IHttpContextAccessor` 默认不注册？

**开口**：它内部用 `AsyncLocal` 传递上下文，有性能开销，而且后台线程/`Task.Run` 里常常拿不到。
**追问**：非要用怎么办？→ 显式 `AddHttpContextAccessor()`，并在最外层把需要的值取出来传下去。

### 7. 一个接口慢，你怎么排查？

**开口**：先看是网络/网关、还是应用内（中间件、DB、下游），用日志耗时埋点和分布式追踪定位。
**追问**：DB 慢怎么办？→ 看执行计划、索引、是否 N+1、是否大结果集，必要时加缓存或分页。

### 8. `DbContext` 能注册成单例吗？

**开口**：不能，它不是线程安全的，而且会无限累积跟踪的实体导致内存暴涨和脏数据。
**追问**：那连接池呢？→ 用 `AddDbContextPool`，池化的是实例复用，不是把实例变成单例。

### 9. `AsNoTracking` 什么时候不能用？

**开口**：需要在同一查询里做标识解析、或者查完要改并保存时。
**追问**：只读为什么还要跟踪？→ 需要「同一条记录在导航属性里出现两次要返回同一实例」的场景。

### 10. N+1 怎么发现和解决？

**开口**：开 EF 日志或拦截器数 SQL 条数；解法是 Include、投影 DTO、分页 + AsSplitQuery。
**追问**：Include 多了会怎样？→ 笛卡尔积膨胀，用 `AsSplitQuery` 拆成多条查询。

### 11. 并发冲突怎么处理？

**开口**：加 RowVersion，提交时版本不匹配抛 `DbUpdateConcurrencyException`，然后重载重试或按业务合并。
**追问**：重试会不会无限循环？→ 要限次 + 退避；业务上不可合并的冲突直接提示用户。

### 12. 生产环境怎么执行迁移？

**开口**：生成 bundle 或幂等脚本，由发布流水线单独执行，而不是应用启动时 `Migrate()`。
**追问**：为什么启动迁移危险？→ 多实例并发执行、迁移失败导致整个服务起不来、DDL 锁表影响可用性。

### 13. 幂等怎么设计？

**开口**：业务唯一号 + 唯一索引兜底，配合状态机只允许单向流转。
**追问**：没有唯一号怎么办？→ 客户端生成幂等键（token），服务端存 Redis/去重表判重。

### 14. 分布式锁你会怎么选？

**开口**：普通业务 Redis `SET NX PX` + Lua 释放；强一致要求用 etcd/ZK。
**追问**：为什么不用 Redlock？→ 依赖时钟与 GC 停顿，强一致场景它不解决问题。

### 15. CAP 在你们项目里怎么落地？

**开口**：大多数业务选 AP + 最终一致，靠 outbox、消息重试、定时对账补齐。
**追问**：什么场景必须 CP？→ 资金账务的关键记账、库存最终扣减这类不能出现中间态的写。

### 16. 超卖是怎么产生的？

**开口**：读-判断-写不是原子操作，两个请求都读到剩余 1 就都通过；缓存与 DB 不同步也会放大。
**追问**：最可靠的兜底？→ `UPDATE ... WHERE stock >= n` 检查影响行数 + 唯一索引，其他都是优化。

### 17. Redis 预扣怎么保证不丢？

**开口**：Lua 一次完成「幂等判断 + 扣减 + 写 outbox」，relay 用 `RPOPLPUSH` 到 processing 再投 MQ，失败可恢复。
**追问**：Redis 挂了怎么办？→ 预扣数据要有落盘/重建机制，最终以 DB 为准做对账。

### 18. 少卖怎么补偿？

**开口**：预扣成功但落库失败 → 补偿队列回滚预扣 + 定时对账发现差异。
**追问**：对账口径怎么定？→ 以预扣流水 vs 订单表 vs 库存表三方比对，差异进人工/自动修复。

### 19. 订单超时关闭三种实现？

**开口**：定时扫表（简单、有延迟）、延迟队列（精确、要中间件）、Redis ZSet 轮询（轻量、要兜底）。
**追问**：扫表怎么避免全表扫描？→ 状态 + 时间建联合索引，只扫待支付且到期的分片。

### 20. 秒杀系统怎么分层？

**开口**：接入层挡（CDN、静态化、限流、验证码）→ 应用层无状态 + Redis 预扣 → 存储层异步批量落库。
**追问**：瓶颈最终在哪？→ 数据库写入，所以核心是「把 QPS 峰值转成匀速写入 + 幂等 + 对账」。

---

# 附录 C：口述版——3 分钟讲清一次请求的旅程

> 面试官问「讲讲你们的后端架构 / 一条请求怎么走完」时用。约 800 字 ≈ 3 分钟。

### 逐字稿

**【开场 · 15 秒】**
我习惯用**一条请求的旅程**来讲 .NET 后端：从 Kestrel 收包，到中间件、路由、过滤器，再到 DI 解析出服务、EF Core 落库，最后返回。这条路线上每个环节都有一个高频面试点。

**【管道 · 45 秒】**
请求进来先进 Kestrel，然后是一条 `RequestDelegate` 组成的**洋葱管道**。顺序就是行为：异常处理必须在最外层，认证在授权之前，`UseAuthorization` 必须夹在 `UseRouting` 和 `UseEndpoints` 之间——否则拿不到端点上的策略元数据。中间件可以短路，所以静态文件、限流这类要放在业务之前。
再往后是路由和过滤器：授权 → 资源 → 模型绑定 → 操作 → 异常 → 结果。想做统一返回体就用结果过滤器，想统一兜异常更推荐中间件，因为它更外层。

**【DI · 40 秒】**
服务实例都来自 DI 容器。三种生命周期我按「有没有请求级状态」来选：无状态单例、DbContext 这种有请求状态用 Scoped、轻量短命用 Transient。最经典的坑是**单例注入 Scoped**——它会被提升成单例，跨请求共享状态；开发环境下 `ValidateScopes` 会直接报错。另外 `IHttpContextAccessor` 默认不注册，就是因为 `AsyncLocal` 有开销且容易在后台线程里拿不到。

**【数据层 · 40 秒】**
数据层是 EF Core。`DbContext` 是 Scoped 且非线程安全，不能并发共享。只读查询我会上 `AsNoTracking`，但需要标识解析时不能关。N+1 靠 Include、投影和 `AsSplitQuery` 解决，靠日志数 SQL 条数来发现。并发冲突用 RowVersion，捕获 `DbUpdateConcurrencyException` 后重载重试。迁移一定要在流水线用 bundle 执行，别在应用启动时 `Migrate()`。

**【高并发与收尾 · 40 秒】**
到了高并发场景，我的顺序是：先缓存挡读，再异步化解耦，再限流降级，最后才是扩机器。库存这类写操作，最终兜底一定是 `UPDATE ... WHERE stock >= n` 加唯一索引；Redis 预扣只是把峰值流量挡在数据库外，落库靠 outbox 加批量事务，缺的数据靠对账补。
如果让我重做，我会先把**日志、指标、链路追踪**补齐——不然上面这些设计出了问题，你连是哪一层慢都说不清。

---

### 压缩版

**一句话版（10 秒）**
> 一条请求 = Kestrel → 中间件 → 路由 → 过滤器 → DI → EF → 返回；每层各有一个必问点。

**30 秒版**
> 管道顺序决定行为，认证在授权前、授权夹在 Routing 和 Endpoints 之间。DI 按「有无请求级状态」选生命周期，单例不能依赖 Scoped。EF 的 DbContext 非线程安全，只读用 AsNoTracking，N+1 靠 Include/投影，并发用 RowVersion 加重试，迁移走流水线。高并发按缓存、异步、限流、降级、扩容的顺序做，库存兜底靠条件更新加唯一索引。

### 时间分配

| 段落 | 时长 | 只说一件事 |
| --- | --- | --- |
| 开场 | 15s | 用「一条请求的旅程」串起来 |
| 管道 | 45s | 洋葱模型 + 顺序 + 过滤器 |
| DI | 40s | 三种生命周期 + 单例注入 Scoped 的坑 |
| 数据层 | 40s | 跟踪 / N+1 / 并发 / 迁移 |
| 高并发与收尾 | 40s | 缓存→异步→限流→扩容；兜底靠条件更新 |

### 被打断时怎么接

| 面试官插话 | 接续句 |
| --- | --- |
| 「中间件顺序说错了会怎样？」 | 「最典型是 CORS 放在认证后面，预检请求直接被 401」 |
| 「你们用 DbContext 池化了吗？」 | 「用了 AddDbContextPool，坑是构造函数不能有请求级依赖」 |
| 「库存为什么不用乐观锁？」 | 「也用过，高冲突下重试率太高，所以秒杀改走 Redis 预扣」 |
| 「线上最怕什么」 | 「最怕缓存和 DB 不一致还没对账：用户看到有货，下单失败」 |
