# 面试资料归档

> 后端岗位（C# / .NET + MySQL + Redis + MQ + 系统/分布式/架构）面试资料集合。九份文档统一采用同一套结构：**正文分章精讲 + 附录 A 背诵卡 + 附录 B 模拟面试 + 附录 C 口述稿**（MySQL / Redis / 分布式 / 系统网络 / 排查 / DDD 另有附录 D 图）。
> 目标：既能当教材精读，也能在面试前 20 分钟快速过一遍。

## 文档清单

| 文档 | 内容 | 规模 | 特别之处 |
| --- | --- | --- | --- |
| [csharp-interview.md](csharp-interview.md) | 类型系统、GC 与内存、集合与 LINQ、并发与异步、**并发异步进阶（原理 + 10 道自测 + FAQ）**、常考手写题 | 39 题 + 10 道自测 + 附录 A/B/C · 1342 行 | 语言层地基，手写题与 async 深挖都在这 |
| [dotnet-interview.md](dotnet-interview.md) | ASP.NET Core、EF Core、分布式与架构、电商场景题 | 33 题 + 附录 A/B/C · 782 行 | 工程落地最全，库存扣减五方案 |
| [mysql-interview.md](mysql-interview.md) | 一条 SQL 的旅程、B+ 树索引与失效、MVCC 与锁、redo/undo/binlog、EXPLAIN 优化、表设计与场景题 | 38 题 + 附录 A/B/C/D · 954 行 | 附录 D 有 4 张图：执行路径 / 两阶段提交 / MVCC / 加锁范围 |
| [redis-interview.md](redis-interview.md) | 快的原因、编码、持久化、过期淘汰、缓存三坑、一致性、主从/哨兵/Cluster、分布式锁、大 key/热 key、场景设计 | 42 题 + 附录 A/B/C/D · 991 行 | 附录 D 有 4 张 Mermaid 时序图 |
| [mq-interview.md](mq-interview.md) | RabbitMQ 模型与可靠性、集群与运维；Kafka 架构、生产者/消费者、ISR/HW、KRaft；选型对比 | 36 题 + 附录 A/B/C · 850 行 | 直达本仓库秒杀链路的真实实现 |
| [distributed-interview.md](distributed-interview.md) | CAP/BASE、六种分布式事务、Raft 共识、注册中心/锁/ID、雪崩防护、多活与可观测性 | 27 题 + 附录 A/B/C/D · 674 行 | 附录 D 有 4 张图：2PC 阻塞、Raft 选举、本地消息表、熔断状态机 |
| [os-network-interview.md](os-network-interview.md) | TCP 握手挥手与可靠传输、五种 IO 模型与 epoll、零拷贝、进程线程协程、排查调优 | 22 题 + 附录 A/B/C/D · 616 行 | 解释「为什么 Redis/Kafka 快」的底层依据 |
| [linux-perf-interview.md](linux-perf-interview.md) | 分层排查方法论、CPU/内存/磁盘/网络指标解读、容器环境陷阱、P99 抖动定位 | 25 题 + 附录 A/B/C/D · 626 行 | 60 秒排查清单 + 「可证伪」记录模板 |
| [ddd-microservices-interview.md](ddd-microservices-interview.md) | 限界上下文与聚合、拆分四维度与四误区、同步异步、CQRS/Saga、绞杀者与扩展-收缩 | 28 题 + 附录 A/B/C/D · 688 行 | 附「本仓库的 DDD 痕迹」可当项目案例 |

配套设计文档仍在上一层 `docs/`：

- [../seckill-1000qps.md](../seckill-1000qps.md) —— 1000 QPS 秒杀方案（Redis outbox + Kafka，含 2 张时序图）
- [../inventory-deduction.md](../inventory-deduction.md) —— 库存扣减方案

---

## 怎么用

### 三条阅读路线

| 时间 | 走法 | 目的 |
| --- | --- | --- |
| **20 分钟** | 九份文档的「附录 A 背诵卡」 | 临考最后一遍，只记结论和数字 |
| **1 天** | 各篇正文 → 篇末「自测清单」 | 建立体系，卡住的地方回正文对应题号 |
| **半天** | 附录 B（20 题模拟 + 追问链）→ 附录 C（3 分钟口述稿） | 练「被追问也不慌」和「能连续讲 3 分钟」 |

### 按岗位选读

| 方向 | 建议顺序 | 理由 |
| --- | --- | --- |
| **C# / .NET 后端** | csharp → dotnet → mysql → redis → mq | 语言与框架是主战场，存储与缓存是必问项 |
| **中间件 / 基础架构** | os-network → linux-perf → redis → mq → distributed | 先懂 IO 与网络，才能解释 Redis/Kafka 为什么快 |
| **架构 / 技术负责人** | ddd → distributed → mysql → mq → redis | 边界、一致性与容量这三件事决定架构成败 |
| **只求稳过** | 九份附录 A → 各篇附录 B 的 Q1~Q10 | 覆盖 80% 的高频问题 |

### 自测方法

1. 先只看每章最后的「自测清单」，对不上答案的题打勾。
2. 遮住答案答一遍，答不出的回正文。
3. 用附录 B 的追问链自查：**能给出一句「开口第一句」+ 一条追问答案**才算过关。
4. 用附录 C 的逐字稿录音，听自己有没有超过规定时长。

---

## 跨文档主题索引

同一个主题常常在几篇里都有，按「最该看的那篇」排前面：

| 主题 | 首选 | 再看 |
| --- | --- | --- |
| 值类型 / struct / Equals | [csharp Q1~Q3](csharp-interview.md) | 附录 A 第 1~3 条 |
| 泛型 / 委托 / 闭包 / yield | [csharp Q8~Q11](csharp-interview.md) | 附录 B Q2/Q3/Q4 |
| GC 与内存泄漏 | [csharp §二](csharp-interview.md) | 附录 B Q5/Q6 |
| 并发 / 异步 / 线程池 | [csharp §五 进阶](csharp-interview.md) | 附录 B Q7~Q14、附录 C 口述稿 |
| ASP.NET Core / EF Core | [dotnet-interview.md](dotnet-interview.md) | 附录 A 第 1~14 条 |
| 索引设计与失效 | [mysql §二](mysql-interview.md) | 附录 A 第 4~12 条、附录 B Q2~Q6 |
| 事务 / MVCC / 锁 | [mysql §三](mysql-interview.md) | 附录 D 图 3~4 |
| 两阶段提交 / redo-undo-binlog | [mysql §四](mysql-interview.md) | 附录 D 图 2 |
| 慢查询 / EXPLAIN | [mysql §五](mysql-interview.md) | 附录 B Q14、Q20 |
| 缓存穿透 / 击穿 / 雪崩 | [redis §四](redis-interview.md#四缓存三大问题与一致性) | 附录 A 第 15~18 条 |
| 缓存与 DB 一致性 | [redis Q18](redis-interview.md) | 附录 B Q8/Q9、附录 D 图 1~2 |
| 分布式锁（实现细节） | [redis §六](redis-interview.md) | 附录 D 图 3、附录 B Q10~Q12 |
| 幂等 / 重复消费 | [mq Q8](mq-interview.md) | [dotnet Q13](dotnet-interview.md)、两篇附录 B 的幂等题 |
| 消息不丢 | [mq Q7](mq-interview.md) | 附录 A RabbitMQ 第 6~8 条、Kafka 第 6~7 条 |
| 顺序性 | [mq Q9 / Q33](mq-interview.md) | 附录 C 的「顺序只在很窄范围内成立」 |
| outbox / 最终一致 | [mq §36](mq-interview.md) | [../seckill-1000qps.md](../seckill-1000qps.md) |
| 共识 / 选主 / 脑裂 | [distributed §二](distributed-interview.md) | [redis §五](redis-interview.md)（哨兵与 Cluster）、[mq Q29](mq-interview.md)（KRaft） |
| 分布式事务 | [distributed Q4~Q7](distributed-interview.md) | [mq §36](mq-interview.md)（outbox 落地） |
| 限流 / 熔断 / 降级 | [distributed Q17](distributed-interview.md) | [redis Q40](redis-interview.md)（限流 Lua） |
| 分布式 ID / 锁的选型 | [distributed Q18~Q19](distributed-interview.md) | [redis §六](redis-interview.md)（Redlock 争议） |
| IO 模型 / epoll / 零拷贝 | [os §二](os-network-interview.md) | [redis Q1](redis-interview.md)、[mq Q17](mq-interview.md)（为什么快） |
| TCP / 连接与端口排查 | [os §一、§四](os-network-interview.md) | 附录 A 第 1~4、18~19 条 |
| 性能排查方法论 / 60 秒清单 | [linux §一](linux-perf-interview.md) | [os §四](os-network-interview.md)（指标解读） |
| 容器排查（cgroup / throttling） | [linux Q23](linux-perf-interview.md) | os 附录 A 第 19 条 |
| DDD 建模（限界上下文 / 聚合） | [ddd §一](ddd-microservices-interview.md) | [dotnet §四](dotnet-interview.md)（订单域落地） |
| 微服务拆分与演进 | [ddd §二、§四](ddd-microservices-interview.md) | [distributed §四](distributed-interview.md) |
| CQRS / 事件溯源 | [ddd Q18~Q19](ddd-microservices-interview.md) | [redis Q18](redis-interview.md)（缓存一致性） |
| 库存扣减 / 超卖 | [dotnet §四](dotnet-interview.md) | [mysql Q36](mysql-interview.md)、[../inventory-deduction.md](../inventory-deduction.md) |
| 秒杀方案 | [../seckill-1000qps.md](../seckill-1000qps.md) | redis 附录 A 第 35 条、mq Q18、dotnet Q20 |

---

## 与仓库代码的对应关系

面试时拿项目举例最加分，这些文档已经和代码对上了：

| 文档里的说法 | 代码位置 |
| --- | --- |
| Redis 只做「票闸」+ Lua 原子预扣 | `t1_frame/src/application/t1_frame.application.abp/Seckill/SeckillService.cs` |
| outbox → processing 的崩溃恢复 | `.../Seckill/SeckillOutboxRelay.cs` |
| 批量消费 + 手动提交 `offset + 1` | `.../Seckill/SeckillKafkaConsumer.cs` |
| 消费幂等（`order_no` / `biz_no` upsert） | `.../Seckill/SeckillBatchWriter.cs`、`.../Handler/StockDeductedHandler.cs` |
| RabbitMQ 示例（可当「找茬题」） | `t1_frame/src/common/t1_frame.core/TaskScheduler/RabbitMQService.cs` |
| ABP 分布式事件 / 模块装配 | `t1_frame/src/webapi/t1_frame.webapi.abp/HostWebHostModule.cs` |

---

## 维护约定

- 新增面试文档放本目录，并**同步更新上面两张表**（清单 + 主题索引）。
- 统一结构：`正文（### 数字. 问题）` + `附录 A 背诵卡` + `附录 B 模拟面试` + `附录 C 口述稿`；需要时加 `附录 D 时序图`。
- 答案只写「能在 30 秒内讲完」的版本，细节放追问里——避免文档变成论文。
- 引用其他文档用相对路径（同目录直接 `xxx.md`，上一层用 `../xxx.md`）。
