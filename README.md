# developer_growing

个人 C# / .NET 学习与实践仓库：ABP 主后端（`t1_frame`）、Avalonia + Prism 桌面端（`t1_frame_wpf`），以及若干独立示例项目。

## 文档

| 文档 | 主题 |
| --- | --- |
| [.NET 经典面试题](docs/dotnet-interview.md) | C# 语言基础、GC、集合与 LINQ、多线程与异步、ASP.NET Core、EF Core、分布式、手写题、业务场景题 |
| [异步编程与并发编程核心知识点](docs/async-concurrency-core.md) | 并发 / 并行 / 异步三者的边界、`async/await` 状态机原理、同步原语选型、死锁与线程池饥饿，附 10 道自测题答案 |
| [1000 QPS 秒杀方案](docs/seckill-1000qps.md) | Kafka + outbox 生产链路、Redis 预扣、不超卖压测实测（1592 QPS @ 200 并发） |
| [库存扣减示例](docs/inventory-deduction.md) | 基于 `t1_frame.webapi.abp` 的电商库存扣减：MySQL 条件更新 / 乐观锁、Redis Lua、分布式事件、MongoDB 审计、Vue 前端 |

## 目录结构

| 路径 | 内容 |
| --- | --- |
| `t1_frame/` | 主后端解决方案：ASP.NET Core + ABP，外加大量独立示例（控制台、Worker、Kafka / RabbitMQ / MassTransit、WinForms、WPF、MVC） |
| `t1_frame_wpf/` | Avalonia + Prism + ReactiveUI 桌面端脚手架 |
| `docs/` | 学习笔记与方案文档（见上表） |
| `openspec/` | OpenSpec 变更提案与规格 |
| `samples/` | 第三方参考仓库压缩包，不参与构建 |

## 构建与测试

两个解决方案相互独立，没有统一构建，命令需指定具体项目或解决方案：

```powershell
dotnet build t1_frame/src/webapi/t1_frame.webapi.abp/t1_frame.webapi.abp.csproj
dotnet build t1_frame/src/core/t1_frame.domain/t1_frame.domain.csproj

dotnet test t1_frame/test/t1_frame.test/t1_frame.test.csproj
dotnet test t1_frame/test/t1_frame.test/t1_frame.test.csproj --filter "FullyQualifiedName~Should_Resolve_Controller"
```

> `t1_frame.test` 的测试夹具标记了 `[Explicit]`，直接 `dotnet test` 只发现不执行，需带 `--filter`。
> `t1_frame.sln` 整体构建会因 `Pomelo.EntityFrameworkCore.MySql` 版本冲突报 `NU1605`，`t1_frame_wpf.sln` 缺少 `build/*.props`，均属已知问题——请按项目单独构建。

更多约定见 [AGENTS.md](AGENTS.md)。
