# RabbitMQ / Kafka 面试精选（含要点与追问）

> 消息队列高频题整理，按「模型 → 可靠性 → 高可用 → 场景」递进。RabbitMQ 与 Kafka 分开成篇，最后给选型对比与本仓库的真实实现。
> 建议用法：先遮住答案自测；「如何保证消息不丢」「如何保证不重复消费」是必背模板，务必练到能一口气说完。

## 目录

- [第一部分 RabbitMQ](#第一部分-rabbitmq)
  - [一、模型与基础](#一模型与基础)
  - [二、可靠性](#二可靠性)
  - [三、集群与运维](#三集群与运维)
- [第二部分 Kafka](#第二部分-kafka)
  - [四、架构与存储](#四架构与存储)
  - [五、生产者](#五生产者)
  - [六、消费者与消费组](#六消费者与消费组)
  - [七、高可用与一致性](#七高可用与一致性)
  - [八、语义与坑](#八语义与坑)
- [第三部分 选型与实践](#第三部分-选型与实践)
- [自测清单](#自测清单)
- [附录 A：15 分钟背诵卡](#附录-a15-分钟背诵卡)
- [附录 B：模拟面试 20 题（含追问链）](#附录-b模拟面试-20-题含追问链)
- [附录 C：口述版——3 分钟讲清 MQ](#附录-c口述版3-分钟讲清-mq)

---

# 第一部分 RabbitMQ

## 一、模型与基础

### 1. AMQP 核心模型

```text
Producer → Connection → Channel → Exchange --(Binding + RoutingKey)--> Queue → Channel → Consumer
                                    │
                                    └── 路由不到 → 丢弃 / Basic.Return / 备用交换机
```

| 概念 | 说明 |
| --- | --- |
| **Connection** | 一条 TCP 连接（带 TLS/认证开销） |
| **Channel** | 连接上的**轻量虚拟通道**，收发消息、声明队列都走 Channel |
| **Exchange** | 交换机，只做路由，不存消息（除非绑定了队列） |
| **Queue** | 真正存消息的地方，FIFO |
| **Binding** | Exchange 与 Queue 的绑定关系 + 路由键 |
| **RoutingKey** | 生产者发消息时带的路由键 |
| **VHost** | 虚拟主机，逻辑隔离（权限、交换机、队列互不可见） |

> 追问：**为什么要 Channel，不能一个线程一个 TCP 吗？** —— TCP 建连/握手/TLS 代价高，一条连接能开成百上千个 Channel；但**Channel 不是线程安全的**，多线程要么各用各的 Channel，要么加锁。生产上一般是「连接池 + 每线程一个 Channel」。

---

### 2. 四种交换机

| 类型 | 路由规则 | 场景 |
| --- | --- | --- |
| `direct` | RoutingKey **完全等于** BindingKey | 按类型/等级精确分发 |
| `fanout` | 忽略 RoutingKey，广播给所有绑定队列 | 广播、通知、缓存刷新 |
| `topic` | 通配符：`*` 匹配**一个**词，`#` 匹配**零个或多个**词 | `order.created.cn`、`log.*.error` |
| `headers` | 按消息 header 的 key-value 匹配（`x-match: all/any`） | 规则复杂、路由键不够用时 |

- 默认交换机 `""`（空字符串）是 **direct**，RoutingKey 等于队列名，所以最简单的用法是 `basicPublish("", "queueName", ...)`。
- 交换机和队列**没有绑定**时，消息默认直接丢弃（除非 `mandatory=true` 触发 `Basic.Return`，或配置了 **备用交换机 AE**）。

> 追问：`topic` 里 `*.error` 能匹配 `order.cn.error` 吗？—— 不能，`*` 只匹配一个词；要 `#.error` 或 `order.*.error`。

---

### 3. 一条消息的完整生命周期

1. 生产者 `basicPublish(exchange, routingKey, props, body)`。
2. Broker 收到后按 exchange + binding 路由到队列；**此时才算「投递成功」**（publisher confirm 的 ack 在这个时刻返回）。
3. 消息按 `delivery_mode` 存内存 / 落盘。
4. Broker 主动 `basicDeliver` 推给订阅的消费者（RabbitMQ 是 **push 模型**）。
5. 消费者处理完 → `basicAck`。未 ack 前消息在 `unacked` 状态。
6. 消费者断开且未 ack → 消息重回 `ready`（`redelivered=true`）。

---

## 二、可靠性

> 这节是面试主战场。答题结构固定为三句：**生产者不丢 → Broker 不丢 → 消费者不丢**，最后补一句「端到端靠幂等兜底」。

### 4. 生产者如何保证消息送到了 Broker？

| 方案 | 做法 | 代价 |
| --- | --- | --- |
| **Publisher Confirm**（推荐） | `ConfirmSelect()` 开启确认模式，Broker 返回 `basic.ack / basic.nack`；可异步回调，也可批量 `WaitForConfirmsOrDie()` | 少量吞吐损失 |
| **事务** | `txSelect → publish → txCommit` | 吞吐下降一个数量级，**生产不用** |
| **mandatory + Return** | 消息路由不到队列时触发 `Basic.Return`，业务可感知并补偿 | 只解决「没路由到」，不解决「Broker 挂了」 |
| 备用交换机（AE） | Exchange 声明时指定 `alternate-exchange`，路由失败转投兜底队列 | 补充手段 |

> 追问：**confirm 的 ack 代表什么？** —— 代表消息已被 exchange 路由到队列（durable 队列下还会落盘）。若路由不到队列，Broker 会**先发 basic.nack 或什么都不做**？准确说：路由不到时 confirm 仍可能 ack（消息被"接受"但丢弃），所以要 `mandatory=true` + `Return` 才能发现。

---

### 5. Broker 如何保证不丢？

三件套缺一不可：

```csharp
// 1. 交换机持久化
channel.ExchangeDeclare("order.ex", ExchangeType.Direct, durable: true);
// 2. 队列持久化（+ 类型：classic / quorum / stream）
channel.QueueDeclare("order.queue", durable: true, exclusive: false, autoDelete: false);
// 3. 消息持久化
var props = channel.CreateBasicProperties();
props.Persistent = true;          // delivery_mode = 2
channel.BasicPublish("order.ex", "order.created", props, body);
```

注意：**持久化 ≠ 立即落盘**，RabbitMQ 是「先写内存 / 页缓存，异步刷盘」，所以真正的保证来自 **durable + persistent + publisher confirm + 至少一个可用副本（quorum queue）**。

**队列类型怎么选**：

| 类型 | 复制方式 | 场景 |
| --- | --- | --- |
| classic | 不复制（镜像队列已废弃，4.0 移除） | 单机、可丢数据 |
| **quorum**（Raft，3.8+） | 多副本强一致，推荐 | 生产默认选择 |
| stream（3.9+） | 日志型，支持大堆积、重复读 | 海量堆积、类似 Kafka 的场景 |

---

### 6. 消费者如何保证不丢？

```csharp
channel.BasicQos(prefetchSize: 0, prefetchCount: 20, global: false);   // 未 ack 上限
var consumer = new EventingBasicConsumer(channel);
consumer.Received += (s, e) =>
{
    try
    {
        Handle(e.Body);
        channel.BasicAck(e.DeliveryTag, multiple: false);       // 处理成功才 ack
    }
    catch
    {
        channel.BasicNack(e.DeliveryTag, multiple: false, requeue: false);  // 进死信，不要无限 requeue
    }
};
channel.BasicConsume("order.queue", autoAck: false, consumer);   // 关掉自动 ack
```

- **autoAck=true 是最常见的丢消息原因**：消息一推出去就删，消费者处理到一半崩了 → 消息没了。
- `requeue: true` 会让消息立刻重回队列再次投递，**毒消息会死循环**，要用「重试次数 + 死信队列」。
- `prefetch` 太大：一个消费者攒一堆，内存高、其他消费者空闲；太小：吞吐上不去。经验值 10~100，按单条处理耗时调。

> 追问：`unacked` 消息会一直占内存吗？—— 会，直到 ack、nack 或消费者断开；`INFO`/管理台里 `messages_unacknowledged` 就是这么来的。

---

### 7. 「如何保证消息不丢」标准答案

```text
① 生产者：publisher confirm（异步 ack）+ mandatory/return + 失败重投本地消息表
② Broker ：durable exchange/queue + delivery_mode=2 + quorum queue 多副本；集群 + 磁盘告警监控
③ 消费者：手动 ack + 处理成功后再 ack + 失败进重试队列/死信队列 + prefetch 限制
④ 兜底  ：消费端幂等（唯一键/去重表/状态机）+ 对账任务
```

**取舍**：可靠性等级越高，吞吐越低。日志/埋点可以「最多一次」，订单/支付必须「至少一次 + 幂等」。

---

### 8. 重复消费与幂等

「至少一次」必然重复，幂等只能做在消费端：

| 方案 | 实现 |
| --- | --- |
| 唯一索引 | 业务单号建唯一索引，重复插入直接冲突忽略 |
| 去重表 / Redis SETNX | `SETNX biz:{id}` + TTL，处理前先抢标记 |
| 状态机 | 只允许 `待支付 → 已支付`，重复消息的状态流转是空操作 |
| upsert | 幂等写（`ReplaceOne(IsUpsert=true)`），后到的覆盖先到的 |

> 本仓库实例：`StockDeductedHandler` 把审计日志按 `biz_no` 做 **MongoDB upsert**，注释就写着「消息重复投递时覆盖而不是新增」。

---

### 9. 消息顺序怎么保证？

- RabbitMQ 只保证**单队列 + 单消费者 + 不并发处理**时有顺序；多消费者并发就是乱序。
- 需要顺序时：
  - 单队列单消费者（吞吐低）。
  - **按业务键拆分队列**（同一个订单号路由到同一队列），队列内单消费者。
  - 消费端加「序列号 + 期望序号」校验，乱序丢弃或缓冲重排。
- 更常见的做法：**用状态机容忍乱序**（比如「已发货」消息先到，就等「已支付」到了再处理）。

---

### 10. 死信队列（DLX）与延迟队列

**死信的三个来源**：

1. 消费者 `basicNack/basicReject` 且 `requeue=false`
2. 消息 TTL 过期
3. 队列超过 `x-max-length`（或 `x-max-length-bytes`）被挤出

```csharp
var args = new Dictionary<string, object>
{
    { "x-dead-letter-exchange", "order.dlx" },
    { "x-dead-letter-routing-key", "order.dead" },
    { "x-message-ttl", 30_000 }        // 队列级 TTL
};
channel.QueueDeclare("order.queue", durable: true, false, false, args);
```

**延迟队列三种实现**：

| 方案 | 原理 | 坑 |
| --- | --- | --- |
| TTL + DLX（经典） | 消息在队列里等到过期 → 转死信 → 被真正消费 | **消息级 TTL 会队头阻塞**：只有队头过期才会被移走，短 TTL 排在长 TTL 后面就一起延迟 |
| `rabbitmq_delayed_message_exchange` 插件 | 交换机内部时间轮，到期才投递 | 需装插件；大量延迟消息占内存 |
| 外部调度（Redis ZSet / 定时任务 / 时间轮） | 到期再发到 MQ | 多一套组件，但最灵活、最可控 |

> 面试高频：**「RabbitMQ 能实现延迟队列吗？」** —— 能，但优先说明「TTL + DLX 的队头阻塞问题」，再给插件方案，最后说大规模场景用 Redis ZSet / 时间轮。

---

## 三、集群与运维

### 11. 集群形态

| 形态 | 数据复制 | 说明 |
| --- | --- | --- |
| 普通集群 | ❌ 只复制元数据 | 队列实际只在一个节点，其他节点只做转发（单点瓶颈/单点丢失） |
| 镜像队列 | ✅ 全量镜像 | 3.9 起标记废弃，4.0 移除，别再用 |
| **Quorum Queue** | ✅ Raft 多副本 | 3.8+，生产首选；需要奇数节点（3/5），不支持非持久化队列 |
| Stream | ✅ 分区日志 | 大堆积 + 可重复消费，类 Kafka |
| Federation / Shovel | 跨集群单向/双向同步 | 跨机房、跨云 |

> 追问：Quorum 队列 3 节点挂 1 个还能写吗？—— 能，Raft 多数派（2/3）；挂 2 个就不可写（只剩少数派）。

---

### 12. 网络分区（脑裂）怎么处理？

```conf
cluster_partition_handling = pause_minority   # 三节点及以上推荐
# autoheal：网络恢复后选一个赢家，其余重启；ignore：不管，人工介入
```

- `pause_minority`：少数派节点**主动暂停**，避免双主写入，恢复后自动回归。
- 双机房部署要小心：可能把其中一整个机房判为少数派而停服。

---

### 13. 内存/磁盘告警与流控

```conf
vm_memory_high_watermark.relative = 0.4
disk_free_limit.absolute = 5GB
```

触发后 Broker 会**阻塞生产者连接**（`connection.blocked` 通知），但消费者仍可消费。

> 面试点：这不是"挂了"，是**背压保护**。堆积时先看是不是消费者跟不上，而不是直接扩内存。

---

### 14. 堆积了怎么办？

排查顺序：

1. 看 `messages_ready` / `messages_unacknowledged` / 消费者数量（管理台或 `rabbitmqctl list_queues name messages consumers`）。
2. 消费者是不是**卡在某个慢处理**（DB 慢查询、下游超时）→ 看消费耗时而不是盲目加机器。
3. `prefetch` 是否过小导致吞吐上不去。
4. 单队列吞吐有天花板 → **拆队列**（按业务键哈希）或者上 Stream/Quorum。
5. 紧急止血：临时扩容消费者、降级非核心逻辑、把积压消息转存到别的介质慢慢消化。

---

### 15. RabbitMQ 的性能与短板

- 单机一般 **万级~十万级** QPS，单队列顺序投递有上限；靠**多队列 + 多消费者**水平扩展。
- 延迟低（微秒~毫秒级），路由能力强，是它相对 Kafka 的最大优势。
- 短板：**堆积能力弱**（内存/磁盘压力大，Quorum 复制放大写放大）、无分区概念、消息消费即删无法回溯、海量日志场景不合适。

---

# 第二部分 Kafka

## 四、架构与存储

### 16. 核心概念一次讲清

| 概念 | 含义 |
| --- | --- |
| Broker | 一个 Kafka 节点 |
| Topic | 逻辑消息分类，物理上分成多个 Partition |
| **Partition** | **并行与有序的最小单位**，只能被同组内一个消费者消费 |
| Replica | 分区副本，`replication.factor` 控制数量 |
| **Leader / Follower** | 读写只走 Leader，Follower 主动 fetch 同步 |
| **AR** | Assigned Replicas，所有副本 |
| **ISR** | In-Sync Replicas，与 leader 保持同步的副本（含 leader） |
| **OSR** | 落后太多的副本（`replica.lag.time.max.ms` 默认 30s 没追上） |
| **LEO** | Log End Offset，每个副本最后一条消息的下一个位置 |
| **HW** | High Watermark = ISR 中最小 LEO，**消费者只能读到 HW 之前** |
| Controller | 集群里负责选主、副本状态变更的那个 Broker |

> 追问：为什么要 HW？—— 保证消费者读到的都是「已被 ISR 确认」的、切主后不会消失的消息（避免读到将来会被截断的数据）。

---

### 17. Kafka 为什么快？

五条，背下来：

1. **顺序写盘**：追加写 segment 文件，磁盘顺序写可达数百 MB/s，接近内存随机写的量级。
2. **页缓存（page cache）**：读写都交给 OS 页缓存，Kafka 自己不做 JVM 堆内缓存，避免 GC 和双份内存。
3. **零拷贝（sendfile）**：消息从页缓存直接 DMA 到 socket，**不经过用户态**，省掉两次拷贝和两次上下文切换。
4. **批量 + 压缩**：`batch.size` / `linger.ms` 攒批，lz4/zstd 压缩，网络和磁盘都省。
5. **分区并行**：一个 topic 多分区分散到多 broker，吞吐线性扩展。

> 追问：零拷贝什么时候失效？—— 开启 TLS/SSL 时数据要加密，必须进用户态；消息格式转换（老版本 down-conversion）也会退化。

---

### 18. 存储结构

```text
topic-order-0/
  00000000000000000000.log        # 消息本体
  00000000000000000000.index      # offset 稀疏索引（默认每 4KB 一条）
  00000000000000000000.timeindex  # 时间戳索引（按时间戳查 offset）
  leader-epoch-checkpoint
```

- 每个 segment 默认 1GB（`segment.bytes`），写满滚动新文件。
- 稀疏索引：不是每条消息都建索引，二分找到最近的索引项再顺序扫，用少量内存换 O(logN) 定位。
- 清理策略：`cleanup.policy=delete`（按 `retention.ms` / `retention.bytes` 过期删除）或 `compact`（按 key 只留最新值，用于 changelog / 状态恢复；`value=null` 是墓碑，标记删除）。

---

## 五、生产者

### 19. acks 三档怎么选？

| acks | 语义 | 丢数据风险 |
| --- | --- | --- |
| `0` | 发出即认为成功，不等任何响应 | 最高（网络抖动就丢） |
| `1` | Leader 写入本地日志就返回 | Leader 挂且未同步 → 丢 |
| `all` / `-1` | ISR 全部写入才返回 | 最低（需配合 `min.insync.replicas`） |

**不丢的三件套**：

```properties
acks=all
replication.factor=3
min.insync.replicas=2
```

> 追问：为什么 `acks=all` 还可能丢？—— 如果 ISR 只剩 1 个副本（其他都挂了），`acks=all` 就等于 `acks=1`。`min.insync.replicas=2` 让 ISR 不足 2 时**直接拒绝写入**（`NotEnoughReplicasException`），用可用性换一致性。

---

### 20. 幂等生产者与事务

**幂等**（`enable.idempotence=true`，3.0 起默认开）：

- Broker 给每个生产者分配 `PID`，每个分区带单调递增 `sequence`。
- Broker 缓存每个 (PID, 分区) 的最后 5 个 batch，重复序号直接丢弃。
- 解决的是**生产者重试导致的重复**，不解决跨会话/跨分区原子。

**事务**（`transactional.id`）：

```java
producer.initTransactions();
producer.beginTransaction();
producer.send(record1);
producer.sendOffsetsToTransaction(offsets, groupId);   // 把消费位点也纳入事务
producer.commitTransaction();
```

实现 **Exactly-Once（consume-transform-produce）**：下游用 `isolation.level=read_committed` 只读已提交事务的消息。

> 追问：EOS 是端到端吗？—— 事务只能覆盖「Kafka 内部 + 位点提交」，如果下游是 MySQL/Redis，仍需自己做幂等。EOS 不是万能药。

---

### 21. 分区器与顺序性

- 有 key：`murmur2(key) % numPartitions` → **同 key 必进同分区** → 同 key 有序。
- 无 key：2.4+ **sticky partitioner**，攒满一个 batch 再换分区，减少请求数、提高压缩率。
- 想改分区数会**打乱 key 与分区的映射**，历史消息的"同 key 有序"会被破坏 —— **分区数尽量一次定好**。

---

### 22. 攒批参数怎么调？

```properties
batch.size=16384          # 单批上限，超过立即发
linger.ms=5               # 最多等 5ms 攒批，权衡延迟与吞吐
compression.type=lz4      # CPU 换带宽
buffer.memory=33554432    # 缓冲区；满了 send() 阻塞 max.block.ms
max.in.flight.requests.per.connection=5   # 幂等场景下 Kafka 会自动限制保证顺序
```

顺序 + 重试的经典坑：`retries>0` 且 `max.in.flight>1` 时，重试可能乱序 → 开幂等（自动处理）或把 in-flight 设为 1。

---

## 六、消费者与消费组

### 23. 消费组与 rebalance

- 同组内**一个分区只能被一个消费者消费**（组内互斥）；不同组之间互不影响（广播语义）。
- **触发 rebalance**：消费者加入/离开、订阅 topic 变化、**分区数变化**、`session.timeout.ms` 超时、`max.poll.interval.ms` 内没 poll（处理太慢被踢）。
- **Eager（默认旧协议）**：撤销所有分区 → 重新分配，全组 STW。
- **Cooperative（增量）**：只迁移需要变动的分区，减少停顿。

分配策略：`Range`（按 topic 连续分，易不均）、`RoundRobin`、`Sticky`（尽量保留上次分配，减少迁移）、`CooperativeSticky`（推荐）。

> 追问：怎么减少 rebalance？—— `group.instance.id`（static membership，重启不踢组）、调大 `max.poll.interval.ms`、控制 `max.poll.records`、用 CooperativeSticky。

---

### 24. 三个超时别搞混

| 参数 | 默认 | 作用 |
| --- | --- | --- |
| `heartbeat.interval.ms` | 3s | 心跳间隔（后台线程发） |
| `session.timeout.ms` | 45s（3.0+） | 多久没心跳判定消费者死亡 |
| `max.poll.interval.ms` | 5min | 两次 `poll()` 的最大间隔，**处理太慢会被踢出组** |

> 高频坑：心跳线程还活着，但业务处理卡了 6 分钟 → 被判死、rebalance、分区被抢走。解决办法是缩短单批处理时间或调大 `max.poll.interval.ms`。

---

### 25. offset 提交

- 位点存在 `__consumer_offsets` 内部 topic（默认 50 分区）。
- **提交的是「下一条要消费的 offset」**（本仓库 `SeckillKafkaConsumer` 里 `result.Offset + 1` 就是这个原因）。
- `enable.auto.commit=true`（默认）：每 `auto.commit.interval.ms`（5s）在 poll 时自动提交 **上一次 poll 返回的位点**。
  - 先提交后处理 → 崩溃丢消息。
  - 先处理后提交 → 崩溃重复消费。
- 生产建议：`enable.auto.commit=false` + 处理成功后**手动提交**（批量提交减少 RPC）。

---

### 26. 消费幂等与重复

Kafka 默认是**至少一次**（at-least-once），重复来自：重试、rebalance 后从上次提交位点重放、手动提交失败。

对策同 RabbitMQ：唯一键、去重表、状态机、upsert。或用事务 + `read_committed` 做 EOS（限于 Kafka 内部链路）。

---

### 27. 消息堆积怎么办？

```bash
kafka-consumer-groups.sh --bootstrap-server localhost:9092 --describe --group seckill
# CURRENT-OFFSET / LOG-END-OFFSET / LAG
```

处理顺序：

1. **先定位瓶颈**：是消费逻辑慢（DB、RPC），还是分区不够（消费者数已经 = 分区数）。
2. 加消费者：**上限 = 分区数**，超过就是白加。
3. 扩分区：只能加不能减，且会破坏 key 的映射与顺序。
4. 提高单条处理效率：批量写库、异步化、去掉同步 IO。
5. 应急：新建高并行度 topic 分流、临时丢弃非关键消息、把积压导出离线处理。

> 追问：LAG 是负数可能吗？—— 会（消费者提交了超过 HW 的位点、或 `__consumer_offsets` 数据异常），说明位点不合法，要查是不是人为 reset。

---

## 七、高可用与一致性

### 28. ISR、HW 与 Leader Epoch

- `replica.lag.time.max.ms=30000`：follower 超过 30s 没追上 → 踢出 ISR。
- `unclean.leader.election.enable=false`（默认）：宁可不可用，也不让落后副本当 leader（否则丢数据）。
- **Leader Epoch（KIP-101）**：给每次 leader 变更编号，follower 截断时按 epoch 对齐，解决「HW 相同但数据不同」导致重启后数据不一致/丢失的问题。

> 一句话总结：**HW 保证读一致，Leader Epoch 保证切主后日志一致**。

---

### 29. Controller 与 ZooKeeper / KRaft

- Controller 负责：分区 leader 选举、ISR 变更处理、副本迁移。
- **ZK 模式**：Controller 靠 ZK 选举，元数据存 ZK，规模大时 ZK 成为瓶颈，且脑裂/元数据不一致难排查。
- **KRaft（KIP-500）**：Kafka 自己用 Raft 管理元数据，2.8 预览、3.3 生产可用、**4.0 移除 ZooKeeper**。
  - 好处：元数据变更不再走 ZK、支持百万级分区、启动更快、部署少一个组件。

> 面试点：现在答「Kafka 依赖 ZK」会被追问，要主动说 KRaft 已经是默认方向。

---

### 30. 分区数怎么定？

- 估算：`目标吞吐 / 单分区吞吐`，单分区一般几 MB/s ~ 十几 MB/s。
- 不少于消费者并发度（否则加消费者没用）。
- 别过多：**每个分区都占文件句柄和内存**、leader 选举变慢、rebalance 变慢（分区迁移是逐个来的）、端到端延迟升高（攒批变小）。
- 经验：中小业务 3~12 个分区起步；分区数一旦确定尽量不动。

---

## 八、语义与坑

### 31. 三种投递语义

| 语义 | 实现 |
| --- | --- |
| At most once | `acks=0` 或先提交 offset 再处理 |
| At least once | `acks=all` + 处理成功后提交（**默认推荐**） |
| Exactly once | 幂等生产者 + 事务 + `read_committed`（仅 Kafka 内部链路） |

> 面试标准答法：**「工程上普遍是至少一次 + 消费幂等，等价于业务层面的恰好一次」**。

---

### 32. 「消息丢失」的三个环节

| 环节 | 原因 | 对策 |
| --- | --- | --- |
| 生产 | `acks=0/1`、缓冲区满丢、异步发送不检查结果 | `acks=all`、`retries`、幂等、检查 `send` 回调 |
| Broker | 未刷盘就宕机、`unclean.leader.election=true`、`min.insync.replicas` 不足 | `replication.factor>=3`、`min.insync.replicas=2`、关掉 unclean 选举 |
| 消费 | 先提交位点再处理、多线程处理异常被吞 | 先处理后提交、异常回退/进重试 topic、幂等 |

---

### 33. Kafka 的延迟消息与死信

- **无原生延迟投递**：常见方案是「多级延迟 topic（1s/5s/30s/1m…）逐级转发」、外部调度（Redis ZSet / 时间轮）、或业务侧定时表。
- **无原生 DLQ**：用 Spring Kafka 的 `DefaultErrorHandler` + `DeadLetterPublishingRecoverer` 把毒消息投到 `topic.DLT`，或自建 `topic.retry.N` + 重试次数 header。
- Kafka 4.0 的 **Share Group（KIP-932）** 改善了「队列式消费」语义（消息级 ack、避免分区空洞），但**不是**延迟投递。

---

### 34. 常见运维命令

```bash
kafka-topics.sh --bootstrap-server localhost:9092 --describe --topic order
kafka-consumer-groups.sh --bootstrap-server localhost:9092 --list
kafka-consumer-groups.sh --bootstrap-server localhost:9092 --describe --group g1        # LAG
kafka-consumer-groups.sh --bootstrap-server localhost:9092 --group g1 --reset-offsets --to-earliest --execute --topic order
kafka-topics.sh --bootstrap-server localhost:9092 --alter --topic order --partitions 12
kafka-topics.sh --bootstrap-server localhost:9092 --describe --under-replicated-partitions
```

重点盯：`under-replicated-partitions`（副本同步有问题）、`offline-partitions`（不可用）、消费组 LAG。

---

# 第三部分 选型与实践

### 35. RabbitMQ vs Kafka

| 维度 | RabbitMQ | Kafka |
| --- | --- | --- |
| 模型 | **Push**（Broker 推给消费者） | **Pull**（消费者主动拉，长轮询） |
| 路由能力 | 强（4 种交换机、header、AE） | 弱（只有 topic + key 分区） |
| 吞吐 | 万级~十万级 | 十万级~百万级 |
| 延迟 | 更低（微秒~毫秒） | 毫秒级（受 `linger.ms` 攒批影响） |
| 堆积能力 | 弱（内存/磁盘压力，Quorum 复制放大） | 强（顺序写盘，可堆 TB 级） |
| 消息保留 | 消费即删，**无法回溯** | 按时间/大小保留，**可按 offset 重放** |
| 顺序 | 单队列单消费者 | 单分区内有序 |
| 延迟消息 / 死信 | 原生 DLX，延迟靠 TTL+DLX 或插件 | 均需自建 |
| 运维复杂度 | 低（单机即可用） | 高（分区/副本/Controller/容量规划） |
| 典型用途 | 业务解耦、任务队列、RPC 异步化、复杂路由 | 日志/埋点/流计算、大数据管道、事件溯源 |

**一句话选型**：**「消息要按规则精准投递、量不大、要低延迟」选 RabbitMQ；「量大、要顺序、要回溯、要接数据平台」选 Kafka。**

---

### 36. 本仓库的真实实现（可直接当项目案例讲）

**秒杀链路：Redis → Outbox → Kafka → 批量落库**

```text
请求 → Redis Lua 原子预扣（库存不足直接拒）
     → RPOPLPUSH  outbox → processing（Redis List，原子转移）
     → SeckillOutboxRelay 投 Kafka（acks=all，EnableIdempotence=true）
     → 删除 processing
     → SeckillKafkaConsumer 批量消费(BatchSize=500) → 批量写库 → 手动提交 offset+1
```

关键设计点（面试可以逐条讲）：

| 设计 | 解决什么 | 代码位置 |
| --- | --- | --- |
| `RPOPLPUSH` outbox→processing | 进程崩溃不丢消息，启动时 `RecoverProcessingAsync` 搬回重投 | `SeckillOutboxRelay.cs` |
| `acks=all` + 幂等生产者 | 生产端不丢 + 重试不重复 | `SeckillOutboxRelay.cs`（ProducerConfig） |
| 批量消费 500 + 手动提交 `offset+1` | 吞吐与「提交的是下一条位点」的语义 | `SeckillKafkaConsumer.cs` |
| 写库失败重试 3 次再提交/回退 | 避免「提交了但没落库」 | `SeckillKafkaConsumer.cs` |
| 按 `order_no` 幂等 | 允许重复投递 | `SeckillService` / 批量写入 |
| 审计日志 `biz_no` upsert | 重复消息覆盖而非新增 | `StockDeductedHandler.cs` |

**一处可以当"找茬题"的示例**：`t1_frame/src/common/t1_frame.core/TaskScheduler/RabbitMQService.cs` 里 `QueueDeclare(durable: false)` + `autoAck: false` + `BasicGet` 轮询 —— 教学示例，生产环境应改为 `durable: true`（交换机/队列/消息都持久化）+ `BasicConsume` 推送 + `BasicQos` 限流，否则 Broker 重启队列就没了。

---

## 自测清单

**RabbitMQ**

- [ ] Connection / Channel / Exchange / Queue / Binding / RoutingKey 各自职责
- [ ] 四种交换机，topic 的 `*` 与 `#` 区别
- [ ] confirm 与事务的区别，为什么生产只用 confirm
- [ ] 持久化三件套 + 为什么"持久化 ≠ 不丢"
- [ ] 手动 ack 的三种响应，`requeue=true` 为什么会死循环
- [ ] prefetch 的作用与调大调小的后果
- [ ] 「如何保证消息不丢」三句话模板
- [ ] 幂等四种实现，能举出项目里的例子
- [ ] 死信的三个来源，延迟队列三种实现及 TTL 队头阻塞问题
- [ ] 普通集群 / 镜像 / Quorum 的区别，Quorum 挂了 1 个节点还能写吗
- [ ] `pause_minority` 解决什么，什么场景下反而危险

**Kafka**

- [ ] Partition / Replica / ISR / LEO / HW / Leader Epoch 串成一句话
- [ ] 为什么快（五条），零拷贝什么时候失效
- [ ] segment + 稀疏索引 + 清理策略（delete / compact）
- [ ] acks 三档，`acks=all` 什么时候仍然会丢
- [ ] 幂等生产者靠什么去重，事务解决什么、不解决什么
- [ ] rebalance 的触发条件、Eager 与 Cooperative 的差别
- [ ] 三个超时的区别，`max.poll.interval` 的经典坑
- [ ] 为什么要手动提交，提交的是哪个 offset
- [ ] 堆积的排查顺序，为什么消费者数不能超过分区数
- [ ] KRaft 与 ZooKeeper 的差异
- [ ] 分区数怎么定，为什么不能随便改
- [ ] Kafka 的延时消息 / 死信怎么落地

**选型**

- [ ] 用三句话讲清 RabbitMQ 与 Kafka 的取舍
- [ ] 能画出并讲通本仓库「Redis 预扣 → outbox → Kafka → 批量落库」链路

---

# 附录 A：15 分钟背诵卡

> 面试前 15 分钟只看这一节。每条都是「一句话能说完」的版本，展开细节回到正文。

### RabbitMQ（15 条）

1. 链路：`Producer → Connection → Channel → Exchange →(Binding+RoutingKey) Queue → Consumer`；**Exchange 只路由不存消息**。
2. Channel 是 TCP 上的轻量虚拟通道，**非线程安全**；生产用「连接池 + 每线程一个 Channel」。
3. 四种交换机：`direct` 精确、`fanout` 广播、`topic` 通配（`*` 一个词 / `#` 多个词）、`headers`。
4. 默认交换机 `""` 是 direct，RoutingKey 就是队列名。
5. 生产端可靠：**publisher confirm（异步 ack）+ mandatory/return**；事务吞吐差，生产不用。
6. Broker 可靠：**exchange durable + queue durable + delivery_mode=2 + quorum queue 多副本**。
7. 消费端可靠：**autoAck=false + 手动 ack + prefetch 限流 + nack 进死信**。
8. 「持久化 ≠ 立即落盘」，真正不丢靠 durable + persistent + confirm + 副本四件套。
9. 至少一次 → **消费端幂等**：唯一索引 / 去重表 / 状态机 / upsert。
10. 顺序：单队列 + 单消费者才有序；要并发有序就**按业务键拆队列**。
11. 死信三来源：`reject/nack(requeue=false)`、TTL 过期、超过 `x-max-length`。
12. 延迟队列：TTL+DLX（**消息级 TTL 会队头阻塞**）/ `delayed-message-exchange` 插件 / Redis ZSet。
13. 集群：普通（只复制元数据）/ 镜像（已废弃）/ **Quorum（Raft，首选）** / Stream（大堆积）。
14. 脑裂用 `pause_minority`；内存磁盘告警 → 生产者 blocked，那是**背压不是故障**。
15. 优势：低延迟 + 强路由；短板：**堆积弱、无回溯、万级吞吐**。

### Kafka（15 条）

1. **Partition 是并行与有序的最小单位**，同组内一个分区只能被一个消费者消费。
2. **ISR** = 与 leader 保持同步的副本（含 leader）；**HW** = ISR 最小 LEO，消费者只读到 HW 之前。
3. **Leader Epoch** 解决「切主后日志截断不一致」（KIP-101）。
4. 为什么快：顺序写 + 页缓存 + sendfile 零拷贝 + 批量压缩 + 分区并行。
5. 存储：segment（默认 1GB）+ 稀疏 `.index`/`.timeindex`；清理 `delete` 或 `compact`。
6. acks：`0` 不等 / `1` leader 落盘 / `all` ISR 全写；不丢三件套 `all + RF=3 + min.insync.replicas=2`。
7. ISR 不足 `min.insync` → **直接拒写**，用可用性换一致性。
8. 幂等生产者：`PID + 每分区单调 seq`，broker 缓存窗口去重（3.0 起默认开）。
9. 事务：跨分区原子 + 位点原子提交 + `read_committed` = EOS；**只覆盖 Kafka 内部链路**。
10. 分区器：有 key 走 `murmur2 % N`（同 key 有序）；无 key 用 sticky 攒批。
11. rebalance 触发：成员进出、订阅变化、**分区数变化**、`session.timeout`、`max.poll.interval` 超时。
12. 三个超时：`heartbeat 3s` / `session 45s` / `max.poll.interval 5min`（**处理慢会被踢出组**）。
13. 提交的是**下一条**位点；自动提交 5s 一次，先提交后处理会丢，生产用手动提交。
14. 堆积：加消费者**上限 = 分区数**；扩分区会打乱 key 映射、破坏有序。
15. KRaft 取代 ZooKeeper（4.0 移除 ZK）；Kafka **无原生延迟消息 / 死信队列**，要自建。

> 一句话对比：**RabbitMQ 抢「路由 + 低延迟 + 业务消息」，Kafka 抢「吞吐 + 顺序 + 可回溯 + 流」**。

---

# 附录 B：模拟面试 20 题（含追问链）

> 每题只给「开口第一句」和追问走向。练法：先自己说 30 秒，再看追问答案。

### 1. 你们为什么用 MQ？带来什么新问题？

**开口**：解耦、异步、削峰；新问题是**一致性、重复、顺序、可观测性**四件事。
**追问**：异步了怎么保证业务最终成功？→ 本地消息表/outbox + 定时补偿 + 对账，别只靠 MQ 的重试。

### 2. 消息丢了怎么排查？

**开口**：按「生产 → Broker → 消费」三段落查日志和指标。
**追问**：怎么证明是消费端丢的？→ 对比 broker 端 offset 与业务落库记录，看是否有「已提交 offset 但无数据」。

### 3. 如何保证不丢？

**开口**：生产者 confirm、Broker 持久化+副本、消费者手动 ack，最后幂等兜底。
**追问**：这样就能 100% 不丢吗？→ 不能，只能把概率压到极低；磁盘损坏要靠多副本跨机架 + 定期演练。

### 4. 如何保证不重复消费？

**开口**：MQ 只能做到至少一次，重复必须业务幂等。
**追问**：幂等键怎么选？→ 业务唯一号（order_no/biz_no），不能选时间戳或消息 ID。

### 5. 消息顺序怎么保证？

**开口**：RabbitMQ 单队列单消费者；Kafka 单分区，同 key 进同分区。
**追问**：分区数能改吗？→ 能加不能减，改了会破坏 key→分区映射，历史顺序失效。

### 6. Kafka 为什么快？

**开口**：顺序写、页缓存、零拷贝、批量压缩、分区并行。
**追问**：零拷贝什么时候失效？→ 开 TLS 或老版本消息格式转换时要进用户态。

### 7. `acks=all` 就一定不丢吗？

**开口**：不一定，ISR 只剩一个副本时 `all` 等价于 `1`。
**追问**：那怎么办？→ `min.insync.replicas=2` + `replication.factor=3`，ISR 不足直接拒写。

### 8. ISR 为什么会收缩？收缩了怎么办？

**开口**：follower 超过 `replica.lag.time.max.ms`（30s）没追上就被踢出 ISR。
**追问**：怎么治？→ 查网络/磁盘 IO/单批过大，必要时降生产速率；持续收缩说明 broker 扛不住。

### 9. HW 和 LEO 的区别？

**开口**：LEO 是每个副本自己的写入末尾，HW 是 ISR 里最小的 LEO，消费者只能读到 HW。
**追问**：为什么要 Leader Epoch？→ 防止「HW 相同但数据不同」的截断错误，KIP-101。

### 10. rebalance 什么时候发生？怎么减少？

**开口**：成员进出、订阅变化、分区数变化、超时被踢。
**追问**：消费慢会导致 rebalance 吗？→ 会，`max.poll.interval.ms` 超时；用 static membership + CooperativeSticky + 控制 `max.poll.records`。

### 11. `session.timeout` 和 `max.poll.interval` 区别？

**开口**：前者是心跳超时（后台线程），后者是 poll 间隔超时（业务线程）。
**追问**：哪个更容易踩坑？→ `max.poll.interval`，心跳还活着但业务卡住，照样被踢。

### 12. 消费位点提交早了会怎样？

**开口**：先提交后处理，崩溃就丢消息。
**追问**：那先处理再提交呢？→ 重复消费，靠幂等消化；要更低重复就缩短批大小、提高提交频率。

### 13. 消息堆积怎么处理？

**开口**：先定位是消费慢还是并行度不够，再决定加消费者/扩分区/优化逻辑。
**追问**：消费者已经等于分区数了还堆？→ 只能扩分区、提升单条处理速度、或另起高并行 topic 分流。

### 14. RabbitMQ 延迟队列怎么做？

**开口**：TTL + DLX 最常用，但**消息级 TTL 有队头阻塞**，所以用队列级 TTL 或插件或 Redis ZSet。
**追问**：为什么消息级 TTL 会阻塞？→ 只有队头消息过期才会被检查移走，后面的短 TTL 也被堵住。

### 15. 死信队列里都是什么消息？

**开口**：被拒绝的、过期的、超队列长度被挤出的；本质是「无法正常投递」的消息。
**追问**：死信要不要一直保留？→ 要有 TTL + 告警 + 人工回放工具，否则就是垃圾场。

### 16. RabbitMQ 集群挂了 1 个节点会丢消息吗？

**开口**：普通集群会（队列数据只在一台）；Quorum 队列不会（Raft 多数派还活着）。
**追问**：3 节点挂 2 个呢？→ 少数派，不能写，等恢复。

### 17. 分布式事务/最终一致怎么落地？

**开口**：本地消息表（outbox）→ 异步投递 → 消费幂等 → 对账兜底。
**追问**：outbox 为什么要 `RPOPLPUSH` 到 processing？→ 崩溃时能恢复，不会「取出来就没了」。

### 18. 为什么秒杀不直接写 MySQL？

**开口**：行锁竞争 + 事务开销把吞吐压到个位数/几十 TPS。
**追问**：那库存一致性怎么保证？→ Redis Lua 原子预扣挡流量，MySQL 单事务批量落库 + 唯一索引兜底，Redis 只是「票闸」。

### 19. Kafka 能做延迟消息吗？

**开口**：没有原生延迟投递；多级延迟 topic、外部调度（Redis ZSet/时间轮）、业务定时表三种做法。
**追问**：Kafka 4.0 的 Share Group 是干这个的吗？→ 不是，它改善的是队列式消费语义（KIP-932），不提供延迟。

### 20. 如果让你重做这套 MQ 架构，会改什么？

**开口**：明确 SLO（丢失/重复/延迟指标化）→ 统一 outbox 组件 → 消费幂等下沉到框架 → 死信与回放工具 → 压测与演练。
**追问**：最容易被忽略的是什么？→ **可观测性**：积压、延迟、重试次数、死信量，没有指标就没有运维。

---

# 附录 C：口述版——3 分钟讲清 MQ

> 面试官说「讲讲你对消息队列的理解」时用这版。**不要背成论文，按段落停顿**，每段一个信息点。
> 语速按 250 字/分钟算，全文约 750 字 ≈ 3 分钟。

### 逐字稿

**【开场 · 15 秒】**
我对 MQ 的理解其实就一句话：它是用**最终一致**换**解耦、异步、削峰**的基础设施。所以用不用、用哪个，前提是先确认业务能不能接受"不是立刻一致"。

**【为什么用 · 40 秒】**
具体是三个价值：解耦——下单不用同步去调库存、积分、短信；异步——非核心链路移出主流程，接口 RT 能从几百毫秒降到几十毫秒；削峰——秒杀十万请求先落队列，后台按数据库的能力慢慢消费。
但代价也很清楚，会多出四个问题：**会不会丢、会不会重、顺序还对不对、出事怎么查**。我平时就是围绕这四件事设计。

**【可靠性 · 60 秒】**
可靠性我按三段讲。
生产端：RabbitMQ 用 publisher confirm 拿到 broker 的 ack，路由不到队列再用 mandatory 加 return 感知；Kafka 用 `acks=all`，配合副本数 3 和 `min.insync.replicas=2`，ISR 不够就直接拒写——用可用性换一致性。
Broker 端：RabbitMQ 是交换机、队列、消息三层都持久化，并且用 Quorum Queue 做 Raft 多副本；Kafka 靠 ISR 和 HW，消费者只能读到已经同步的部分。
消费端：关掉 autoAck，处理成功再 ack，失败进死信；Kafka 是处理完再手动提交，而且提交的是**下一条**位点。
最后一句最重要：以上加起来也只能做到**至少一次**，端到端不重复必须靠消费端幂等——我们项目就是用业务唯一号做 upsert，重复消息覆盖而不是新增。

**【顺序与堆积 · 40 秒】**
顺序只在很窄的范围内成立：RabbitMQ 是单队列加单消费者，Kafka 是单分区。要"并发还有序"，就把同一个业务键路由到同一个队列或分区，用并行度换吞吐。
堆积我的判断顺序是：先看是消费逻辑慢，还是并行度不够。Kafka 的消费者数上限就是分区数，加多了是白加；而扩分区会打乱 key 到分区的映射，历史顺序就废了，所以我倾向于一次把分区数定够。

**【选型 · 30 秒】**
选型上我是这么分的：**RabbitMQ 强在路由和低延迟**，适合业务消息、任务队列、RPC 异步化；**Kafka 强在吞吐、顺序和可回溯**，适合日志、埋点、数据管道。
我们项目是两条都用：业务事件走 RabbitMQ，秒杀链路走 Redis outbox 加 Kafka，因为它要的是高吞吐和可重放。

**【落地案例 · 35 秒】**
秒杀那条链路可以说明我怎么把上面这些落地：Redis 用 Lua 原子预扣库存，同一脚本里把消息 push 到 outbox；relay 用 RPOPLPUSH 把它挪到 processing，再投 Kafka；消费者批量五百条落一次库，成功后才提交位点。
三个崩溃点都有兜底：投递失败整批放回 outbox，relay 挂了启动时从 processing 恢复重投，消费者在提交前挂掉就从上次位点重放——靠订单号幂等，重复也无所谓。

**【收尾 · 15 秒】**
如果让我重做一遍，我会先把四个指标打出来：**堆积量、端到端延迟、重试次数、死信量**。没有这些指标，MQ 就是个黑盒，出事只能靠猜。

---

### 压缩版

**一句话版（10 秒）**
> MQ 用最终一致换解耦、异步、削峰；落地就盯四件事：不丢、不重、有序、可观测。

**30 秒版**
> 我们用它做解耦和削峰，代价是引入了最终一致。可靠性按生产、Broker、消费三段做：confirm、持久化加副本、手动 ack，最后靠业务幂等兜住重复。顺序只在单队列或单分区里保证，需要并发有序就按业务键路由。选型上业务消息用 RabbitMQ，高吞吐可回溯用 Kafka。线上盯堆积、延迟、重试和死信四个指标。

### 时间分配

| 段落 | 时长 | 只说一件事 |
| --- | --- | --- |
| 开场 | 15s | 一句话本质 + 代价 |
| 为什么用 | 40s | 三个价值 + 四个新问题 |
| 可靠性 | 60s | 生产 / Broker / 消费 三段 + 幂等兜底 |
| 顺序与堆积 | 40s | 有序的范围 + 堆积判断顺序 |
| 选型 | 30s | 两条技术栈各自的强项 |
| 案例 | 35s | 秒杀链路 + 三个崩溃点 |
| 收尾 | 15s | 四个指标 |

### 被打断时怎么接

| 面试官插话 | 接续句 |
| --- | --- |
| 「你怎么保证不丢？」 | 「这块我正要展开，分三段——」直接跳到可靠性段 |
| 「那重复消费呢？」 | 「这就是我刚说的幂等兜底，具体三种做法：唯一索引、去重表、状态机」 |
| 「你们到底用哪个？」 | 「两个都用：业务事件 RabbitMQ，秒杀 Kafka，原因是吞吐和可回溯」 |
| 「举个你们线上的问题」 | 「最典型的是消费慢触发 rebalance，`max.poll.interval` 超时，分区被抢走——后来靠控制批大小和 static membership 解决的」 |
| 「你觉得 MQ 最大的坑是什么」 | 「把 MQ 当数据库用：以为消息不会丢、以为能回溯、以为顺序是全局的」 |

