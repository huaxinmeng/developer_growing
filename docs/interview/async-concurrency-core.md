# 异步编程与并发编程核心知识点

> 面向 C# / .NET 的异步与并发整理。先讲概念边界与原理，再给 10 道自测题的完整答案。
> 建议用法：先读第一~四节建立框架，再用第五节自测；答不出的回到对应小节。

## 目录

- [一、三个概念：并发 / 并行 / 异步](#一三个概念并发--并行--异步)
- [二、异步编程的核心](#二异步编程的核心)
- [三、并发编程的核心](#三并发编程的核心)
- [四、两者的交叉点（事故高发区）](#四两者的交叉点事故高发区)
- [五、10 道自测题答案](#五10-道自测题答案)
- [六、追问与澄清（FAQ）](#六追问与澄清faq)
- [七、只记三句话](#七只记三句话)

---

## 一、三个概念：并发 / 并行 / 异步

它们是**三个正交维度**，日常对话里经常被混着用：

| 概念 | 回答的问题 | 手段 | 是否占线程 |
| --- | --- | --- | --- |
| **并发 Concurrency** | 多个任务在**同一时间段**内推进 | 时间分片、事件循环、状态机 | 不一定 |
| **并行 Parallelism** | 多个任务在**同一时刻**真正同时执行 | 多核 + 多线程 | 是，线程数 = 并行度 |
| **异步 Asynchrony** | 发起后**不等结果**就返回，结果好了再通知 | 回调 / Task / await | **不是**，等待期间线程被释放 |

关键认知：

- **异步不等于多线程**。纯 I/O 的 `await` 是「线程让出去，等下再回来」，全程可能只用 1 个线程跑几千个并发请求。
- **并发不等于并行**。单核也能并发（靠切换），但无法并行。
- **并行一定并发**，反过来不成立。

一句话概括：**异步解决「等待浪费」，并发解决「同时推进」，并行解决「算得慢」。**

---

## 二、异步编程的核心

### 1. 本质是状态机，不是线程

`async/await` 编译后是一个 `IAsyncStateMachine`：遇到未完成的 `await` 就 `return`，把后续代码塞进 continuation，等内核 I/O 完成（Windows 是 IOCP）后由线程池线程恢复执行。

所以：

- `await` 一个已完成的 Task **不会**切换线程（同步跑完，最快路径）。
- `await` 真正的 I/O **会**释放当前线程。
- 恢复时**不保证**回到原线程（除非有 `SynchronizationContext`）。

### 2. 什么时候才该异步

**只有 I/O 密集**才值得异步：网络、数据库、文件、消息队列、HTTP。

CPU 密集的活写成 `async` 毫无意义——它本来就在烧线程，异步只是多套了一层状态机。

```csharp
// ❌ 假异步：阻塞 I/O 包了层 Task，线程还是被占着
public Task<string> ReadAsync() => Task.Run(() => File.ReadAllText(path));

// ✅ 真异步
public Task<string> ReadAsync() => File.ReadAllTextAsync(path);

// ✅ CPU 密集才用 Task.Run
var result = await Task.Run(() => HeavyCompute(data));
```

### 3. 六个必须记住的规则

1. **`async void` 只用于事件处理器**。它无法被 await、无法捕获异常（异常直接抛到 `SynchronizationContext`，进程可能崩）。其他一律 `async Task`。
2. **不要 `.Result` / `.Wait()` / `.GetAwaiter().GetResult()` 阻塞异步代码**。
   - UI / 老 ASP.NET 有 `SynchronizationContext` 时 → **死锁**（主线程等 Task，Task 等主线程回来继续）。
   - 现代 ASP.NET Core 无上下文 → 不会死锁，但**线程池饥饿**：请求线程全在阻塞等，池子涨不动，吞吐雪崩。
3. **`ConfigureAwait(false)` 只写在不关心上下文的库里**。UI 框架、需要 `HttpContext` / `AsyncLocal` 之外上下文的地方别乱加。ASP.NET Core 里它只是微优化，不是正确性问题。
4. **异步会传染**。一旦底层是 async，整条调用链都得 async。这是好事——不要在中间某层「截断」成同步。
5. **异常在 await 处抛出**，不是调用处。`Task.WhenAll` 只抛第一个，要全量用 `task.Exception.InnerExceptions` 遍历（.NET 里 `await` 会解包 `AggregateException`）。
6. **`CancellationToken` 一路透传**，别吞掉。创建关联令牌用 `CancellationTokenSource.CreateLinkedTokenSource`。

### 4. 进阶工具

| 工具 | 用途 | 注意 |
| --- | --- | --- |
| `ValueTask` | 高频、常同步完成的路径省一次分配 | **禁止** await 两次、禁止并发 await、禁止 `.Result` |
| `IAsyncEnumerable<T>` | 流式返回（分页、SSE、大结果集） | `await foreach` + `WithCancellation`，天然背压 |
| `TaskCompletionSource` | 把回调式 API 包成 Task | 用 `RunContinuationsAsynchronously` 防同步续体炸栈 |
| `Parallel.ForEachAsync` | 带并发度的异步批处理（.NET 6+） | 内部就是 SemaphoreSlim 那套 |
| `Channel<T>` | 生产者/消费者 | 用 `BoundedChannelOptions` 做背压，别用无界队列 |

---

## 三、并发编程的核心

### 1. 共享可变状态是一切麻烦的根源

并发 bug 的三大类：

- **竞态条件**：`if (x == null) x = new ...`（check-then-act）
- **丢失更新**：`counter++` 编译成读-改-写三步，非原子
- **内存可见性 / 重排**：一个线程写了，另一个线程看不到（需要 `volatile`、`Interlocked`、锁来建立 happens-before）

理论根子是**内存模型**：没有同步原语，编译器和 CPU 都可以重排、可以缓存。`lock` / `Interlocked` / `Volatile.Read` 同时解决互斥和可见性。

### 2. 同步原语选型

| 原语 | 场景 | 坑 |
| --- | --- | --- |
| `lock` (Monitor) | 短临界区、同步代码 | **可重入**、**不能跨 await**（`SemaphoreSlim` 才行） |
| `SemaphoreSlim` | 限流、异步互斥 | `await WaitAsync()` 必须配 `try/finally { Release() }` |
| `Interlocked` | 计数器、CAS、状态机标志 | 只解决单变量原子性，不解决多变量一致性 |
| `ReaderWriterLockSlim` | 读多写少 | 比 lock 慢，别用在微秒级临界区 |
| `ConcurrentDictionary` | 并发字典 | `GetOrAdd` 的工厂**可能被执行多次**，有副作用就危险 |
| `ImmutableXxx` | 彻底免锁 | 每次修改产生新对象，写多则 GC 压力大 |
| `Channel<T>` | 线程/任务间传数据 | 优先于 `BlockingCollection` |
| `async/await` + 无共享 | 最推荐的「并发」 | 结构上消灭共享状态，比任何锁都好 |

### 3. 死锁的四个必要条件

互斥、占有且等待、不可剥夺、循环等待。破任何一个即可：

- **统一加锁顺序**（最实用）
- **用超时**：`Monitor.TryEnter(timeout)`、`SemaphoreSlim.Wait(timeout)`
- **不要在持锁时调用外部代码 / 回调 / await**

### 4. 线程池是有限资源

- 线程池线程数受 `ThreadPool.SetMinThreads` 和注入速率限制，**突然的阻塞会触发饥饿**：队列堆积、延迟从毫秒级跳到秒级。
- 判断信号：`ThreadPool.PendingWorkItemCount` 持续不为 0、`dotnet-counters` 里 threadpool queue length 上涨。
- 药方：**别阻塞**（这是唯一根治），必要时调高 MinThreads 只是止血。

### 5. 结构化并发

- `Task.WhenAll` → 全部完成（失败也要等全部结束再抛）
- `Task.WhenAny` → 超时 / 抢答模式
- **关联取消**：父操作取消 → 子操作全取消
- **别 fire-and-forget**：裸 `_ = DoAsync()` 会丢异常。要么 await，要么挂个 `ContinueWith` 记日志，要么用后台队列（`IHostedService` / Channel）

---

## 四、两者的交叉点（事故高发区）

1. **`lock` 里 await** → 编译不过，改成 `SemaphoreSlim`。
2. **`SemaphoreSlim` 忘了 `finally` 释放** → 永久挂死。
3. **`AsyncLocal` 依赖**：日志 Scope、`HttpContext`、ABP 的 `ICurrentUser` / UoW 都靠 `ExecutionContext` 跨 await 流动。**能流动，但 `Task.Run` 里新建的执行流看不到外层的值**（`AsyncLocal` 会随执行上下文复制，`Task.Run` 是复制点，之后的修改不回传）。
4. **`ConfigureAwait(false)` 后访问 `HttpContext`/UI 控件** → 上下文丢失。
5. **并发度失控**：`Task.WhenAll(10000 个请求)` 直接把下游或连接池打爆。要限流：

```csharp
using var sem = new SemaphoreSlim(10);
var tasks = ids.Select(async id =>
{
    await sem.WaitAsync(ct);
    try { return await client.GetAsync(id, ct); }
    finally { sem.Release(); }
});
await Task.WhenAll(tasks);
```

6. **EF Core / DbContext 不是线程安全的**：一个 `DbContext` 不能并发用，`Task.WhenAll` 里共用一个上下文必炸。ABP 里靠 UoW 作用域隔离，并行任务要各自开 UoW。

### 本仓库的具体落点（t1_frame）

- ABP 应用服务是 `async virtual Task`，**整条链（Controller → AppService → Repository → EF Core）都该是 async**，别在中间 `.Result` 掉。ABP 的拦截器（UoW、权限、审计）依赖 `AsyncLocal`，同步阻塞会破坏上下文流动。
- 迁移里有 `async virtual Task` 方法时用 `await _repo.GetListAsync()`（ABP 提供的异步仓储），不要 `GetList()` 同步版。
- 测试（NUnit）里 `async Task` 测试方法不要写成 `async void`，否则失败不会计入结果。

---

## 五、10 道自测题答案

### 1. `await` 一个已完成的 Task，会切换线程吗？

**不会。**

编译器生成的 awaiter 先检查 `IsCompleted`；为 `true` 就直接从状态机继续往下执行，走的是同步快速路径：

```csharp
var t = Task.FromResult(42);
var v = await t;        // 完全同步执行，无 continuation 调度，无线程切换
```

只有 `IsCompleted == false` 时，才把自己挂成 continuation（注册回调）、`return` 出让线程，等 I/O 完成后由线程池恢复。

推论：`await` 本身不保证「异步」，也不保证「换线程」。想强制出让用 `await Task.Yield()`。

> 「让出线程」这个说法容易误解成「释放线程」或「UI 线程没让出」，见 [第六节 FAQ 2](#六追问与澄清faq)。

### 2. 为什么 `Task.Result` 在老 ASP.NET 里死锁，在 ASP.NET Core 里只是「变慢」？

**差别在有没有 `SynchronizationContext`。**

老 ASP.NET / WinForms / WPF 有上下文，`await` 之后的续体必须 `Post` 回到**那个上下文**执行。你用 `.Result` 把上下文唯一的处理线程堵死 → 续体永远排不上队 → 互等 → 死锁。

一条死锁链长这样：

```
请求线程 ──阻塞等──> Task 完成
                      ↑
         续体要回请求上下文执行 ──> 上下文被请求线程占着
```

ASP.NET Core 从 1.0 起**没有** `SynchronizationContext`（`SynchronizationContext.Current == null`），续体随便找个线程池线程跑就行，所以不会死锁。

但代价是**线程池饥饿**：阻塞的线程白占着，线程池注入速率有限（默认约每 0.5–1 秒才加 1 个线程），并发一上来就在队列里排队，表现为「CPU 不高但延迟暴涨、超时」。

所以准确说法是：**老框架是 hang，新框架是 throughput collapse。** 两者都该避免。

> 更细的机制（`SynchronizationContext.Post` 各环境行为、「ASP.NET Core 不会死锁」的前提条件、常见误解）见 [第六节 FAQ 1](#六追问与澄清faq)。

### 3. `async void` 的异常去哪了？

**没有任何 Task 承载它，它被直接抛到捕获的上下文上，默认等于让进程崩掉。**

- 有 `SynchronizationContext` → 抛给该上下文 → 通常变成未处理异常
- 没有（线程池）→ 未处理异常直接终止进程
- `TaskScheduler.UnobservedTaskException` **收不到**（那是给被遗弃的 Task 用的）
- `try/catch` 只能写在 `async void` 方法**内部**才有效；调用方包 try/catch 完全没用
- 测试框架里 `async void` 测试会「提前通过」或直接崩 runner

这是它只允许出现在事件处理器（`button.Click += async (s, e) => ...`）的原因——那时代码语义就是「发了就不管」。

### 4. 为什么 `lock` 里不能 `await`？

**因为 `Monitor` 是线程亲和的。**

`lock` 展开成 `Monitor.Enter(obj)` / `finally { Monitor.Exit(obj); }`，锁的归属记在**线程**上。而 `await` 之后续体可能在**另一个线程**恢复执行，那时 `Monitor.Exit` 由非持有线程调用 → `SynchronizationLockException`。

编译器直接禁止（`CS1996: Cannot await in the body of a lock statement`），不给你犯错的机会。

就算能编译，后果也很糟：锁被跨 await 持有，可能持锁几百毫秒，期间所有抢这把锁的线程全堵在线程池里 → 池子耗尽 + 死锁。

替代：

```csharp
private readonly SemaphoreSlim _gate = new(1, 1);

await _gate.WaitAsync(ct);
try
{
    await DoSomethingAsync();
}
finally
{
    _gate.Release();   // 别忘了 finally
}
```

`.NET 9` 的 `System.Threading.Lock`（`lock (l) { }` 新语义）也不能跨 `await`。

### 5. `ConcurrentDictionary.GetOrAdd` 的工厂为什么不能有副作用？

**因为它不保证只执行一次，而且它的返回值可能被丢弃。**

典型实现是乐观读 → 失败则**先算好值** → 再加锁/CAS 插入。多个线程同时 miss 时，每个线程都会跑一遍工厂，只有一个的值被插入，其余全丢。

```csharp
// ❌ 三个并发请求可能各发一条消息，只有一条进字典
dict.GetOrAdd(key, k => { _mq.Publish("created"); return new Conn(); });
```

正确姿势：

```csharp
// 用 Lazy 保证「真正创建」只发生一次
var conn = dict.GetOrAdd(key, k => new Lazy<Conn>(() => CreateConn(k))).Value;

// 或者先 TryAdd，失败了再读
if (!dict.TryAdd(key, CreateConn(key)))
    conn = dict[key];
```

顺带一句性能坑：`GetOrAdd(key, k => ...)` 的 lambda 每次调用都分配一个闭包 + 委托，热路径上要用 `GetOrAdd(key, static k => ..., arg)` 这种带参数的重载。

### 6. 异步方法里 `using` 的 `DbContext` 什么时候释放？

**在 `using` 作用域结束时释放，也就是那个 `}` 处——此时作用域内的所有 `await` 都已经完成。**

```csharp
async Task DoAsync()
{
    using var db = new MyDbContext();   // ① 创建
    await db.SaveAsync();               // ② 挂起/恢复
    await db.QueryAsync();              // ③
}                                        // ④ 这里同步 Dispose
```

几个必须清楚的细节：

- **抛异常也会释放**（编译器生成的 `finally` 兜底）
- **在 `using` 里 `return` / `throw`，先释放再离开**，不会泄漏
- `Dispose()` 是**同步**的，会在「恢复执行的那个线程」上跑。想避免连接关闭时的 sync-over-async，用 `await using`（`DbContext` 实现了 `IAsyncDisposable`）
- **反面典型**：`return DoAsync();` 而不 await 就离开作用域 → 后台任务还在用已经 Dispose 的上下文，报 `ObjectDisposedException`（且异常无人接）
- DI 注入的 scoped `DbContext` **不要自己 Dispose**，容器在 scope 结束时会处理；只 Dispose 自己 `new` 出来的

ABP 里的 UoW 就是 scoped 生命周期，`Task.WhenAll` 并行时**每个并行分支要各自开 UoW**，共用一个 `DbContext` 必炸（非线程安全）。

### 7. 线程池饥饿的表现是什么，怎么定位？

**核心信号：CPU 不高，但队列在涨、线程数也在涨。**

| 现象 | 说明 |
| --- | --- |
| 延迟从 ms 级跳到秒级，吞吐骤降 | 请求排队 |
| `cpu-usage` 只有 20–40%，却撑不住 QPS | 线程在**等**不是在算 |
| `threadpool-queue-length` 持续 > 0 且上涨 | 饥饿的直接证据 |
| `threadpool-thread-count` 缓慢爬升 | 池子在疯狂注入，但追不上 |
| 大量超时、连接池耗尽、`TaskCanceledException` | 次生灾害 |

定位命令：

```bash
dotnet-counters monitor -p <pid> System.Runtime
# 盯：cpu-usage / threadpool-queue-length / threadpool-thread-count
#     / monitor-lock-contention-count / exception-count

dotnet-stack report -p <pid>      # 抓调用栈，看是不是一堆线程卡在 .Result / Wait / lock
dotnet-dump collect -p <pid>      # 更细的分析，clrstack -all 数阻塞点
```

栈里如果反复出现 `Task.Wait`、`Task<T>.Result`、`GetAwaiter().GetResult()`、`Monitor.Enter`，基本就实锤了。常见根因：sync-over-async、在 async 链路里做长 CPU 计算、锁粒度太大、限流没做。

### 8. `Task.WhenAll` 里 3 个任务 2 个失败，`await` 会抛几次异常？

**只抛 1 次，而且抛的是第一个失败任务的原始异常（不是 `AggregateException`）。**

```csharp
var all = Task.WhenAll(t1, t2, t3);   // t2、t3 都炸
try { await all; }
catch (Exception ex) { /* ex 是 t2 的那个异常，t3 的看不到 */ }
```

要点：

- `all.Exception` 其实是 `AggregateException`，里面**包含全部两个**失败；`await` 的 awaiter 只取 `InnerExceptions[0]` 重抛
- 想拿全量：`catch (AggregateException ae) { ae.Flatten().InnerExceptions }`，或先 `await` 完成再用 `all.Exception.InnerExceptions` 遍历
- `WhenAll` **会等所有任务跑完**（不管成功失败），不会「一个失败就短路」
- 不 `await` 也不读 `.Exception` 的话，靠 `TaskScheduler.UnobservedTaskException` 在 GC 时才发现——那时已经很难查了

要 fail-fast 就该用 `Task.WhenAny` + 遍历，或者关联 `CancellationTokenSource`，第一个失败就取消其他。

### 9. CPU 密集任务加 `async` 有意义吗？

**单纯加 `async`：没意义。** 异步的价值是「等待时把线程还回去」，而 CPU 密集任务从不等待，线程一直在算——包一层 `async` 只是多一个状态机的分配和一次 `MoveNext`，反而更慢。

要区分三种动机：

| 你真正想要的 | 正确手段 | 是不是异步 |
| --- | --- | --- |
| UI 不卡 | `await Task.Run(...)` | 不，是**换线程** |
| 多核加速 | `Parallel.For` / `Parallel.ForEachAsync` / PLINQ | 不，是**并行** |
| 可取消、可组合、不炸栈 | 包成 `Task` + `CancellationToken` | 勉强算，但无性能收益 |
| 服务端吞吐 | **没有手段**，只能优化算法或少算 | — |

唯一有点意义的场景是「长 CPU 任务里主动让出」：`await Task.Yield()` 或分片处理，避免单个任务长期霸占线程池线程导致别人饿死。这是**公平性**收益，不是性能收益。

一句话：**`async` 不会让计算变快，`Task.Run` 不会让计算变快，只有并行和更好的算法会。**

### 10. `ValueTask` 为什么不能 await 两次？

**因为它可能是个「一次性凭证」，底层 `IValueTaskSource` 常来自对象池——第一次 await 之后就被回收复用给别的操作了。**

典型生命周期：

```
池子里借出 → 第 1 次 await 消费结果 → GetResult() 里归还池子 → 被另一个异步操作借走
                                          ↓
你第 2 次 await → 读到的是别人的状态 → 返回错值 / InvalidOperationException / 静默错乱
```

官方约束四条：

1. **只能 await 一次**
2. **不能并发 await**
3. **完成前不能 `.Result` / `.GetAwaiter().GetResult()`**（可能阻塞，甚至对某些实现直接抛）
4. **拿到后要立刻消费**，别存字段、别跨方法传

确实要多次读：

```csharp
ValueTask<int> vt = ...;
Task<int> t = vt.AsTask();     // 转成 Task，随便 await 多少次（但丢了省分配的意义）
// 或
var vt2 = vt.Preserve();       // 明确表达「要复用」，内部换成可复用的源
```

例外：**结果型** `ValueTask`（`new ValueTask<int>(42)`，内部就是个值）await 多次是安全的——但你不能在调用点保证对方返回的是哪种，所以规则统一按「只用一次」执行。

要不要用 `ValueTask` 的判据很简单：**这个方法在热路径上、且大概率同步完成**，比如缓存命中、`TryRead` 之类。普通业务代码一律 `Task`，收益是负的（多了一层判断和心智负担）。

---

## 六、追问与澄清（FAQ）

### FAQ 1：能不能理解为「UI 线程不受线程池管控」，所以 `.Result` 才死锁？

**对了一半，但这不是成因。**

「UI 主线程不是线程池线程」这个事实本身成立——它是进程启动时创建的主线程，有自己的消息循环，`ThreadPool` 管不到它。但线程池既没调度它、也没阻塞它，所以「管辖关系」不是关键。

真正的因果是：

> **`await` 之后的续体被强制调度回「那条正在被阻塞的线程」执行。** `.Result` 把线程堵住，续体还必须回到这条线程 → 循环等待。

反证很直接：控制台程序也没有 `SynchronizationContext`，`.Result` 却不会死锁。**决定性因素是「有没有 `SynchronizationContext`」，而不是「线程归谁管」。**

#### `SynchronizationContext` 是续体调度策略，不是线程

`await` 完成后的续体这样回到指定位置：

```csharp
var ctx = SynchronizationContext.Current;          // await 时捕获
if (ctx != null)
    ctx.Post(state => continuation(state), null);  // 投递回去
else
    ThreadPool.QueueUserWorkItem(...);             // 随便找个池线程
```

不同实现的 `Post` 语义完全不同：

| 环境 | 上下文 | `Post` 实际行为 |
| --- | --- | --- |
| WinForms | `WindowsFormsSynchronizationContext` | `Control.BeginInvoke` → 塞进 UI 消息队列，**只有 UI 线程会抽** |
| WPF | `DispatcherSynchronizationContext` | `Dispatcher.BeginInvoke` → 同上 |
| ASP.NET Framework | `AspNetSynchronizationContext` | 排进该请求的上下文队列，**该请求同时只允许一个线程处理** |
| Console / ASP.NET Core / 后台服务 | `null` | 直接丢给线程池 |

前三种的续体最终都依赖「某条特定线程继续把事情做下去」，而这条线程正被 `.Result` 阻塞——它不抽队列，续体就永远排着。

```
UI / 请求线程 ── Task.Result 阻塞 ──┐
                                    │  等 Task 完成
Task 完成 ──> 续体 Post 回 该线程 ───┘  但线程在阻塞，抽不了队列
```

标准循环等待，和线程池没关系。

#### 这个类比在老 ASP.NET 上已经不成立

老 ASP.NET **没有 UI 线程**，它是多线程的线程池处理模型，请求线程就来自 `ThreadPool`。它照样死锁，是因为 `AspNetSynchronizationContext` 要求「同一请求的续体串行执行，且需要原请求线程交还控制权」。把请求线程钉死，该请求的内部队列就永远等不到人来处理。

所以准确表述是：

- **WinForms / WPF**：单线程消息泵被阻塞，续体排不上队。
- **老 ASP.NET**：请求上下文被阻塞，续体排不上队。
- 共性是「续体绑在一条已阻塞的线程上」，不是「线程池管不到 UI 线程」。

#### ASP.NET Core 为什么「只是变慢」

`SynchronizationContext.Current == null`，续体走 `ThreadPool.QueueUserWorkItem`，任何空闲池线程都能跑，构不成循环等待。代价转移到了三处：

1. **阻塞的线程本可以服务其他请求**，可用并发度下降；
2. **线程池扩容很慢**：从 MinThreads 往上靠 hill-climbing，注入速率大约每秒 1–2 个线程量级，追不上请求涌入；
3. **排队级联**：延迟从 ms 到秒 → 客户端超时重试 → 请求量翻倍 → 更堵。

症状是「**CPU 打不满，但 QPS 撑不住**」。这是吞吐崩塌，不是卡顿。

#### 重要修正：「ASP.NET Core 不会死锁」是有条件的

条件就是「线程池还能再注入线程」。

极端情况下所有池线程都阻塞在 `Monitor.Wait` 等 Task，没有任何线程能跑续体——此时 **ASP.NET Core 照样卡死**，CPU 归零、进程活着但不响应。

| | 老 ASP.NET / WinForms / WPF | ASP.NET Core / Console |
| --- | --- | --- |
| `SynchronizationContext` | 有 | `null` |
| 续体去哪 | 特定的那条线程 | 任意池线程 |
| 阻塞那条线程 | 续体永远排不上 → 循环等待 | 续体照跑，只是少了一条线程 |
| 典型结果 | **确定性死锁**（必现） | **吞吐崩塌 / 线程池饥饿**（概率性，量大也可能卡死） |
| 修复 | 全链路 async，或至少 `ConfigureAwait(false)` | 全链路 async（别无他法） |

#### 两个常见误解

1. **「`ConfigureAwait(false)` 能修死锁」**——它只是不让续体回原线程，绕过了循环等待。续体里访问 UI 控件照样炸（`InvalidOperationException: 调用线程无法访问此对象`）。这是绕过，不是修复。
2. **「这是 ASP.NET 特有的问题」**——不是。xUnit 有 `MaxConcurrencySyncContext`，NUnit 等测试框架也会安装同步上下文，在里面 `.Result` 一样死给你看。**判断依据永远是：此刻 `SynchronizationContext.Current` 是不是 null，以及它把续体投递到哪。**

---

### FAQ 2：`await` 是「让出线程」的手段，可 UI 线程不也让出去了吗？

**对，UI 线程确实也让出去了。** 准确的说法应该是：

> **`await` 让出的是「当前线程的执行权」——把它交还给它的调度者。**

UI 线程交还给消息循环，服务端线程交还给线程池。两边都让了，但**让的方式和收益完全不同**。

#### 机制上「让出」就是一次 `return`

```csharp
public async Task FooAsync()
{
    DoA();
    await bar.BazAsync();   // ← 未完成时，方法在此处 return
    DoC();
}
```

`return` 意味着**调用栈被逐层弹出**，那条线程的栈帧里不再有这个 async 方法。于是线程空出手来，去做它原本该做的下一个任务：

| 环境 | 线程让出后去干什么 |
| --- | --- |
| WinForms / WPF | 回到 `GetMessage` / `Dispatcher.Run`，**继续抽消息队列**（绘制、输入、其他续体） |
| ASP.NET Core | 回到线程池队列，**领下一个请求** |
| Console | 回到线程池队列，**领任意工作项** |

所以 UI 线程让出后并没有「闲着」，它回消息泵继续干活；服务端线程则是被复用去干别人的活。

#### 但两种「让出」性质不同

| 维度 | UI 线程 | 服务端线程池线程 |
| --- | --- | --- |
| 让出后归谁 | 消息循环（**只有它能用**） | 线程池（**任何人都能用**） |
| 续体回到哪里 | **必须回到原来那条线程**（靠 `Post`） | 任意池线程皆可 |
| 是否真的「释放」 | **不是**，只是「借出」，还要还回来 | **是**，交出去就不再属于你 |
| 让出的收益 | **响应性**（界面不卡） | **吞吐**（少线程扛更多并发） |
| 并发度上限 | 1（CPU 侧永远只有一个 UI 线程） | 随线程数扩展 |
| 资源占用 | 一条专有线程常驻（空闲时阻塞在 `GetMessage`，0 CPU） | 池线程可被完全复用 |

一句话：**UI 线程的让出是「我把手腾出来但不交出去」，服务端线程的让出是「我还给公家了」。**

这也解释了为什么 `.Result` 在 UI 上必然死锁——那条线程是**专属**的，你占住它，消息泵就没人驱动，续体永远回不来。而服务端「占住」的线程只是从池子里借的，池子还能再给一条（直到给不出来为止）。

#### 由此产生的四个修正

**1. `await` 不一定让出。** 若 Task 已完成（缓存命中、`Task.FromResult`），`IsCompleted == true`，状态机压根不 `return`，一次线程切换都没有。「让出」是条件行为。

**2. 「等」不等于「让」。** UI 上最好用的对照组：

```csharp
await Task.Delay(3000);   // UI 线程让出 → 界面照样拖动、动画照跑
Thread.Sleep(3000);       // UI 线程被独占 → 界面冻结 3 秒，未响应
```

**3. UI 上的 `async` 不产生吞吐收益，只产生响应性收益。** 它让界面不卡，但**不会让桌面程序「同时算得更快」**——CPU 侧并发度仍是 1，想并行还得 `Task.Run` 到线程池。这正印证了第一节那句话：**异步 ≠ 并发**。

**4. 续体回到同一线程是债，不是福利。** 代价是所有续体排队走同一条消息队列、彼此串行；UI 线程忙时续体的执行时机不可控（可能延迟几十上百毫秒）。所以库里写代码才要 `ConfigureAwait(false)`——能不去挤那条独木桥就别挤。

#### 修正后的表述

> **`await` 是把当前线程的「执行权」交还给它的调度者（消息循环 / 线程池），等操作完成后再把后续代码作为续体重新排入调度队列。它从不创建线程，也不保证切换线程，更不保证「释放」资源——收益完全取决于「谁接收了这次让出」。**

---

## 七、只记三句话

1. **`await` 是「让出线程」的手段，不是「开线程」的手段。**
2. **所有并发 bug 都来自共享可变状态；`lock` 只解决同步代码的互斥，跨 await 用 `SemaphoreSlim`。**
3. **阻塞异步代码（`.Result` / `.Wait()` / `async void` / 跨 await 持锁）是这套体系里 90% 事故的来源。**

---

## 相关文档

- [.NET 经典面试题](dotnet-interview.md)
- [秒杀 1000 QPS](../seckill-1000qps.md)
- [库存扣减](../inventory-deduction.md)
