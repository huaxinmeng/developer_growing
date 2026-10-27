# 面试资料归档

> 后端岗位（C# / .NET + Redis + MQ）面试资料集合。四份文档统一采用同一套结构：**正文分章精讲 + 附录 A 背诵卡 + 附录 B 模拟面试 + 附录 C 口述稿**（Redis 另有附录 D 时序图）。
> 目标：既能当教材精读，也能在面试前 20 分钟快速过一遍。

## 文档清单

| 文档 | 内容 | 规模 | 特别之处 |
| --- | --- | --- | --- |
| [dotnet-interview.md](dotnet-interview.md) | C# 语言、GC、集合/LINQ、多线程与异步、ASP.NET Core、EF Core、分布式、手写题、电商场景 | 72 题 · 1138 行 · 10 章 | 覆盖最广，是其他三份的地基 |
| [async-concurrency-core.md](async-concurrency-core.md) | 并发 / 并行 / 异步的概念边界、`async/await` 原理、`Task`/`ValueTask`、线程安全与常见事故 | 9 题 + 10 道自测 · 553 行 · 7 节 | 专治「`await` 到底释放不释放线程」这类追问 |
| [redis-interview.md](redis-interview.md) | 快的原因、编码、持久化、过期淘汰、缓存三坑、一致性、主从/哨兵/Cluster、分布式锁、大 key/热 key、场景设计 | 42 题 + 附录 A/B/C/D · 991 行 | 附录 D 有 4 张 Mermaid 时序图 |
| [mq-interview.md](mq-interview.md) | RabbitMQ 模型与可靠性、集群与运维；Kafka 架构、生产者/消费者、ISR/HW、KRaft；选型对比 | 36 题 + 附录 A/B/C · 850 行 | 直达本仓库秒杀链路的真实实现 |

配套设计文档仍在上一层 `docs/`：

- [../seckill-1000qps.md](../seckill-1000qps.md) —— 1000 QPS 秒杀方案（Redis outbox + Kafka，含 2 张时序图）
- [../inventory-deduction.md](../inventory-deduction.md) —— 库存扣减方案

---

## 怎么用

### 三条阅读路线

| 时间 | 走法 | 目的 |
| --- | --- | --- |
| **20 分钟** | 四份文档的「附录 A 背诵卡」 | 临考最后一遍，只记结论和数字 |
| **1 天** | 各篇正文 → 篇末「自测清单」 | 建立体系，卡住的地方回正文对应题号 |
| **半天** | 附录 B（20 题模拟 + 追问链）→ 附录 C（3 分钟口述稿） | 练「被追问也不慌」和「能连续讲 3 分钟」 |

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
| 缓存穿透 / 击穿 / 雪崩 | [redis §四](redis-interview.md#四缓存三大问题与一致性) | 附录 A 第 15~18 条 |
| 缓存与 DB 一致性 | [redis Q18](redis-interview.md) | 附录 B Q8/Q9、附录 D 图 1~2 |
| 分布式锁 | [redis §六](redis-interview.md) | 附录 D 图 3、附录 B Q10~Q12 |
| 幂等 / 重复消费 | [mq Q8](mq-interview.md) | redis、mq 两篇附录 B 的幂等题 |
| 消息不丢 | [mq Q7](mq-interview.md) | 附录 A RabbitMQ 第 6~8 条、Kafka 第 6~7 条 |
| 顺序性 | [mq Q9 / Q33](mq-interview.md) | 附录 C 的「顺序只在很窄范围内成立」 |
| outbox / 最终一致 | [mq §36](mq-interview.md) | [../seckill-1000qps.md](../seckill-1000qps.md) |
| 秒杀方案 | [../seckill-1000qps.md](../seckill-1000qps.md) | redis 附录 A 第 35 条、mq Q18 |
| 并发 / 异步 / 线程池 | [async-concurrency-core.md](async-concurrency-core.md) | dotnet-interview 多线程章 |
| ASP.NET Core / EF Core | [dotnet-interview.md](dotnet-interview.md) | — |

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
