# C# 经典面试题（含要点与代码）

> 面向 C# **语言层面**的高频面试题：类型系统、GC 与内存、集合与 LINQ、并发与异步，以及常考手写题。
> 每题给出**要点答案**与**常见追问**；建议先遮住答案自测，标记答不出的题。
> 平台与框架部分（ASP.NET Core / EF Core / 分布式 / 电商场景）见 [dotnet-interview.md](dotnet-interview.md)。

## 目录

- [一、C# 语言基础](#一c-语言基础)
- [二、内存管理与 GC](#二内存管理与-gc)
- [三、集合与 LINQ](#三集合与-linq)
- [四、多线程与异步](#四多线程与异步)
- [五、并发与异步进阶（原理与自测）](#五并发与异步进阶原理与自测)
- [六、常考手写题](#六常考手写题)
- [七、自测清单](#七自测清单)
- [附录 A：15 分钟背诵卡](#附录-a15-分钟背诵卡)
- [附录 B：模拟面试 20 题（含追问链）](#附录-b模拟面试-20-题含追问链)
- [附录 C：口述版——3 分钟讲清 C# 与并发](#附录-c口述版3-分钟讲清-c-与并发)

---

## 一、C# 语言基础

### 1. 值类型和引用类型有什么区别？

| 维度 | 值类型 | 引用类型 |
| --- | --- | --- |
| 存储 | 变量本身存数据；局部变量在栈，字段内联在对象中 | 变量存堆对象地址 |
| 赋值 | 拷贝整个值 | 拷贝引用 |
| 继承 | 不支持（除 `object` 装箱） | 支持 |
| 默认值 | `0` / `false` / `null`（可空） | `null` |
| 可为 null | 需 `Nullable<T>` | 天然可空（引用类型） |

**装箱 / 拆箱**：值类型转 `object` 或接口会在堆上分配并拷贝（装箱），反向需类型检查（拆箱）。

```csharp
int i = 42;
object o = i;      // 装箱：堆上分配
int j = (int)o;    // 拆箱
```

高频触发点：`ArrayList`、把 struct 当接口使用、`string.Format` 传 struct、非泛型集合。

---

### 2. `struct` 和 `class` 怎么选？

- **struct**：值语义、无继承、不能为 null（除非 `Nullable<T>`）、适合小而短命的数据（坐标、金额、范围）。
- **class**：引用语义、支持继承与多态、可空。
- 可变 struct 是坑：属性修改可能作用在副本上，推荐 `readonly struct`。

```csharp
public readonly struct Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }
}
```

> 追问：struct 作为 `Dictionary` 的键可行吗？可以，但必须重写 `Equals`/`GetHashCode`，否则走反射式比较，性能差。

---

### 3. `==` 和 `Equals` 的区别？

- `==` 是运算符，可重载，默认引用比较（`string` 重载为值比较）。
- `Equals` 是 `object` 的虚方法，默认引用比较；值类型默认按字段逐位比较。
- 重写 `Equals` 必须同时重写 `GetHashCode`，否则 `Dictionary` / `HashSet` 会失效。

```csharp
public sealed class User
{
    public int Id { get; init; }

    public override bool Equals(object? obj) => obj is User u && u.Id == Id;

    public override int GetHashCode() => Id.GetHashCode();
}
```

---

### 4. `const` 和 `readonly` 的区别？

| | `const` | `readonly` |
| --- | --- | --- |
| 求值时机 | 编译期 | 运行期 |
| 可赋类型 | 基元类型、`string`、`null` | 任意类型 |
| 隐式 static | 是 | 否 |
| 跨程序集 | 值被内联，改值需重新编译引用方 | 正常引用 |

---

### 5. `ref`、`out`、`in` 的区别？

- `ref`：进出都要先初始化。
- `out`：进入时不必初始化，方法内必须赋值（多返回值场景）。
- `in`：只读引用，避免大 struct 拷贝，调用方可不加关键字。

---

### 6. 抽象类和接口的区别？

- **单继承 vs 多实现**；抽象类可有字段、构造函数、受保护成员；接口只描述契约。
- C# 8+ 接口支持默认实现，但接口不能有实例字段。
- 语义上：接口表达“能做什么”（能力），抽象类表达“是什么 + 共享代码”。

---

### 7. `virtual` / `override` / `new` 的区别？

`override` 参与多态（按运行时类型），`new` 只是隐藏（按编译期类型）。经典输出题：

```csharp
class Base { public virtual void Foo() => Console.WriteLine("Base.Foo"); }
class Derived : Base { public override void Foo() => Console.WriteLine("Derived.Foo"); }
class Hider : Base { public new void Foo() => Console.WriteLine("Hider.Foo"); }

Base a = new Derived(); a.Foo();  // Derived.Foo
Base b = new Hider();   b.Foo();  // Base.Foo（隐藏不参与多态）
```

---

### 8. 委托和事件的区别？

事件是对委托字段的封装：外部只能 `+=` / `-=`，只有声明类能触发。否则外部可以 `obj.Event = null` 清空所有订阅。

```csharp
public class OrderService
{
    public event EventHandler<OrderCreatedEventArgs>? OrderCreated;

    protected virtual void OnOrderCreated(OrderCreatedEventArgs e)
        => OrderCreated?.Invoke(this, e);
}
```

---

### 9. 泛型的好处？协变与逆变？

- 好处：编译期类型安全、避免装箱、代码复用、性能更好（`List<int>` 无装箱）。
- 约束：`where T : class / struct / new() / 基类 / 接口 / unmanaged / notnull`。
- `out T` 协变：`IEnumerable<string>` → `IEnumerable<object>`。
- `in T` 逆变：`IComparer<object>` → `IComparer<string>`。

---

### 10. 什么是闭包？循环变量捕获的坑？

Lambda 捕获外部变量时，编译器生成一个类把变量提升为字段。

```csharp
var actions = new List<Action>();
for (int i = 0; i < 3; i++)
{
    actions.Add(() => Console.WriteLine(i)); // 捕获同一个 i
}
actions.ForEach(a => a()); // 输出 3 3 3

// 修复：循环内拷贝
for (int i = 0; i < 3; i++)
{
    int copy = i;
    actions.Add(() => Console.WriteLine(copy)); // 0 1 2
}
```

> `foreach` 自 C# 5 起每次迭代新建变量，因此 `foreach (var x in list) actions.Add(() => Console.WriteLine(x));` 是安全的。

---

### 11. `yield return` 的原理？

编译器生成状态机（`IEnumerable` / `IEnumerator`），**延迟执行**，每次 `MoveNext` 才推进到下一个 `yield`。适合大数据流式处理，避免一次性构造大集合。

```csharp
public static IEnumerable<int> ReadNumbers(Stream stream)
{
    using var reader = new StreamReader(stream);
    string? line;
    while ((line = reader.ReadLine()) != null)
    {
        yield return int.Parse(line);
    }
}
```

---

### 12. `IDisposable` 和 `using` 的机制？

`using` 编译为 `try/finally`，在 `finally` 中调用 `Dispose`。标准 Dispose 模式：

```csharp
public class ResourceHolder : IDisposable
{
    private bool _disposed;

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing)
        {
            // 释放托管资源
        }
        // 释放非托管资源
        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~ResourceHolder() => Dispose(false);
}
```

> 追问：`IAsyncDisposable` + `await using`（用于 `Stream`、`DbContext` 等异步释放场景）。

---

### 13. `string` 为什么不可变？拼接怎么做？

不可变保证线程安全、可安全作为哈希键、支持字符串驻留（interning）。频繁拼接用 `StringBuilder`，避免 O(n²) 的中间字符串分配。

---

### 14. `Span<T>` / `Memory<T>` 解决什么问题？

- 零分配的连续内存视图，可切片（`Slice`）而不拷贝。
- `Span<T>` 是 `ref struct`，只能在栈上，**不能跨 `await`**；异步场景用 `Memory<T>`。
- 典型用途：解析协议、字符串处理、高性能序列化。

---

## 二、内存管理与 GC

### 15. .NET 的 GC 是怎么工作的？

- **分代**：Gen0（新对象，回收最频繁）→ Gen1（缓冲）→ Gen2（长寿对象）。
- 算法：标记 → 清除 → 压缩；Gen0/Gen1 采用复制式，速度快。
- **LOH**（大对象堆）：≥ 85000 字节的对象直接进 Gen2，默认不压缩（易产生碎片）。
- 触发：分配预算耗尽、显式 `GC.Collect()`、系统内存压力。
- 模式：Workstation（客户端）vs Server（多线程并行回收，服务端推荐）；后台 GC 降低停顿。

---

### 16. 什么情况会造成内存泄漏？

- 事件订阅未取消（长生命周期对象持有短生命周期对象）。
- 静态集合 / 缓存无上限。
- `Timer`、`Task`、`CancellationTokenSource` 未释放。
- 频繁 `new HttpClient`（连接与句柄耗尽）。
- 非托管资源未 `Dispose`。
- 闭包长期持有大对象。

排查工具：`dotnet-counters`、`dotnet-dump` + `analyze`、`dotnet-trace`、PerfView、VS 诊断工具。

---

### 17. `WeakReference` 有什么用？

允许对象被 GC 回收而不阻止回收，适合缓存、事件聚合器（弱事件模式）。

```csharp
var weak = new WeakReference<BigObject>(new BigObject());
if (weak.TryGetTarget(out var obj))
{
    // 仍存活
}
```

---

### 18. `GC.SuppressFinalize` 为什么要在 `Dispose` 里调用？

对象已确定性释放，无需再进入终结器队列，避免对象多存活一轮 GC 并重复执行终结器。

---

## 三、集合与 LINQ

### 19. 常见集合的适用场景？

| 集合 | 底层 | 特点 |
| --- | --- | --- |
| `List<T>` | 动态数组 | 随机访问 O(1)，扩容约 2 倍，中间插入 O(n) |
| `Dictionary<K,V>` | 哈希表 | 均摊 O(1)，扩容 rehash，键需正确实现哈希 |
| `HashSet<T>` | 哈希表 | 去重、存在性判断 |
| `LinkedList<T>` | 双向链表 | 已知节点时插入/删除 O(1)，缓存不友好，实际少用 |
| `SortedDictionary` | 红黑树 | 有序，操作 O(log n) |
| `ConcurrentDictionary` | 分段/无锁 | 线程安全，适合高并发缓存 |

---

### 20. `IEnumerable` 和 `IQueryable` 的区别？

- `IEnumerable`：内存中执行（LINQ to Objects）。
- `IQueryable`：持有**表达式树**，由 Provider 翻译为 SQL 在数据库执行。

```csharp
// 好：过滤下推到数据库
var list = db.Users.Where(u => u.Age > 18).ToList();

// 坏：先全表加载，再内存过滤
var all = db.Users.ToList().Where(u => u.Age > 18);
```

> 追问：`AsEnumerable()` 之后的操作都在客户端执行；`AsQueryable()` 只是接口转换，不代表一定翻译成 SQL。

---

### 21. LINQ 延迟执行的坑？

- 查询在枚举时才执行；多次枚举会重复查询（含数据库往返）。
- 闭包变量在执行时取值，可能拿到“最新值”。
- 需要立即执行用 `ToList()` / `ToArray()` / `Count()`。

```csharp
int threshold = 10;
var query = users.Where(u => u.Age > threshold);
threshold = 50;          // 影响 query
var result = query.ToList(); // 实际按 50 过滤
```

---

### 22. `First` / `FirstOrDefault` / `Single` 的区别？

- `First`：无匹配抛异常；`FirstOrDefault`：返回默认值。
- `Single`：必须**恰好一个**，0 个或多个都抛异常；`SingleOrDefault`：0 个返回默认值，多个抛异常。

---

## 四、多线程与异步

### 23. `async/await` 的原理？

编译器把方法改写成状态机：遇到未完成的 `await` 就保存状态并返回，注册续延；完成后在线程池（或捕获的同步上下文）继续执行。

**关键点：`async` 不会自动开新线程**。IO 密集型场景是释放线程；CPU 密集型才用 `Task.Run`。

---

### 24. `async void` 为什么危险？

- 异常无法被调用方捕获（抛到 `SynchronizationContext`，通常直接崩进程）。
- 无法 `await`，调用方不知道何时完成。
- 只应出现在事件处理器中。

---

### 25. `.Result` / `.Wait()` 为什么会死锁？

在单线程同步上下文（WinForms / WPF / 老 ASP.NET）中，`await` 的续延要回原上下文，而原线程被 `.Result` 阻塞 → 互相等待。

```csharp
// 死锁示意（UI 线程）
var data = GetDataAsync().Result; // GetDataAsync 内部 await 后要回 UI 线程
```

解决：全链路 async；类库中使用 `ConfigureAwait(false)`。

---

### 26. `Task` 和 `Thread` 的区别？

- `Thread` 是 OS 线程，创建/切换昂贵。
- `Task` 是抽象的工作单元，默认跑在线程池，支持延续、组合（`WhenAll`）、取消、异常聚合。

---

### 27. `lock` 的本质与注意事项？

编译为 `Monitor.Enter` / `Monitor.Exit`（try/finally）。

注意：
- 不要 `lock(this)`、`lock(typeof(X))`、lock 字符串——外部可能锁同一对象导致死锁。
- 不要 lock 值类型（装箱后每次都是新对象，锁不住）。
- 锁内不要做耗时操作或 `await`。

---

### 28. 死锁的四个必要条件？怎么避免？

互斥、占有且等待、不可抢占、循环等待。

避免手段：统一加锁顺序、`Monitor.TryEnter` 超时、缩小锁粒度、无锁结构（`Interlocked`、`Concurrent*`、`Channel`）。

---

### 29. `Interlocked`、`volatile`、`lock` 的区别？

- `Interlocked`：原子操作（自增、CAS），无锁，性能最好。
- `volatile`：保证可见性并禁止重排，但**不保证原子性**。
- `lock`：互斥 + 内存屏障，最重，可保护复合操作。

```csharp
private int _counter;
public void Increase() => Interlocked.Increment(ref _counter);
```

---

### 30. `Task.WhenAll` 和 `WhenAny`？

- `WhenAll`：等全部完成，异常会聚合在 `Task.Exception` 中。
- `WhenAny`：等第一个完成（超时控制常用）。

```csharp
var tasks = ids.Select(id => httpClient.GetStringAsync($"/api/{id}")).ToArray();
var results = await Task.WhenAll(tasks); // 并发发起，不要循环里逐个 await
```

---

### 31. `CancellationToken` 怎么用？

协作式取消：把 token 一路传下去，循环/耗时操作中检查。

```csharp
public async Task WorkAsync(CancellationToken ct)
{
    using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
    cts.CancelAfter(TimeSpan.FromSeconds(5));

    await Task.Delay(10_000, cts.Token);
    ct.ThrowIfCancellationRequested();
}
```

---

### 32. 线程池饥饿是怎么产生的？

大量同步阻塞（`.Result`、`Thread.Sleep`、同步 IO）占满线程池，而线程池注入新线程有速率限制 → 吞吐骤降、响应变慢。解决：异步化、避免阻塞、必要时 `ThreadPool.SetMinThreads` 兜底。

---

### 33. `SemaphoreSlim` 和 `Semaphore` 的区别？

- `SemaphoreSlim`：轻量、进程内、支持 `WaitAsync`，适合异步限流。
- `Semaphore`：基于内核对象，可跨进程，开销更大。

```csharp
private static readonly SemaphoreSlim _gate = new(3);
await _gate.WaitAsync();
try { /* 最多 3 个并发 */ }
finally { _gate.Release(); }
```

---

## 五、并发与异步进阶（原理与自测）

> 这一章比「四、多线程与异步」更深一层：先讲并发 / 并行 / 异步的边界与运行时原理，再用 10 道自测题和 FAQ 把常见误解掰开。
> 建议：读完第四章再来做自测；答不出的回到 5.1~5.4。

### 5.1 三个概念：并发 / 并行 / 异步

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

### 5.2 异步编程的核心

#### 1. 本质是状态机，不是线程

`async/await` 编译后是一个 `IAsyncStateMachine`：遇到未完成的 `await` 就 `return`，把后续代码塞进 continuation，等内核 I/O 完成（Windows 是 IOCP）后由线程池线程恢复执行。

所以：

- `await` 一个已完成的 Task **不会**切换线程（同步跑完，最快路径）。
- `await` 真正的 I/O **会**释放当前线程。
- 恢复时**不保证**回到原线程（除非有 `SynchronizationContext`）。

#### 2. 什么时候才该异步

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

#### 3. 六个必须记住的规则

1. **`async void` 只用于事件处理器**。它无法被 await、无法捕获异常（异常直接抛到 `SynchronizationContext`，进程可能崩）。其他一律 `async Task`。
2. **不要 `.Result` / `.Wait()` / `.GetAwaiter().GetResult()` 阻塞异步代码**。
   - UI / 老 ASP.NET 有 `SynchronizationContext` 时 → **死锁**（主线程等 Task，Task 等主线程回来继续）。
   - 现代 ASP.NET Core 无上下文 → 不会死锁，但**线程池饥饿**：请求线程全在阻塞等，池子涨不动，吞吐雪崩。
3. **`ConfigureAwait(false)` 只写在不关心上下文的库里**。UI 框架、需要 `HttpContext` / `AsyncLocal` 之外上下文的地方别乱加。ASP.NET Core 里它只是微优化，不是正确性问题。
4. **异步会传染**。一旦底层是 async，整条调用链都得 async。这是好事——不要在中间某层「截断」成同步。
5. **异常在 await 处抛出**，不是调用处。`Task.WhenAll` 只抛第一个，要全量用 `task.Exception.InnerExceptions` 遍历（.NET 里 `await` 会解包 `AggregateException`）。
6. **`CancellationToken` 一路透传**，别吞掉。创建关联令牌用 `CancellationTokenSource.CreateLinkedTokenSource`。

#### 4. 进阶工具

| 工具 | 用途 | 注意 |
| --- | --- | --- |
| `ValueTask` | 高频、常同步完成的路径省一次分配 | **禁止** await 两次、禁止并发 await、禁止 `.Result` |
| `IAsyncEnumerable<T>` | 流式返回（分页、SSE、大结果集） | `await foreach` + `WithCancellation`，天然背压 |
| `TaskCompletionSource` | 把回调式 API 包成 Task | 用 `RunContinuationsAsynchronously` 防同步续体炸栈 |
| `Parallel.ForEachAsync` | 带并发度的异步批处理（.NET 6+） | 内部就是 SemaphoreSlim 那套 |
| `Channel<T>` | 生产者/消费者 | 用 `BoundedChannelOptions` 做背压，别用无界队列 |

---

### 5.3 并发编程的核心

#### 1. 共享可变状态是一切麻烦的根源

并发 bug 的三大类：

- **竞态条件**：`if (x == null) x = new ...`（check-then-act）
- **丢失更新**：`counter++` 编译成读-改-写三步，非原子
- **内存可见性 / 重排**：一个线程写了，另一个线程看不到（需要 `volatile`、`Interlocked`、锁来建立 happens-before）

理论根子是**内存模型**：没有同步原语，编译器和 CPU 都可以重排、可以缓存。`lock` / `Interlocked` / `Volatile.Read` 同时解决互斥和可见性。

#### 2. 同步原语选型

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

#### 3. 死锁的四个必要条件

互斥、占有且等待、不可剥夺、循环等待。破任何一个即可：

- **统一加锁顺序**（最实用）
- **用超时**：`Monitor.TryEnter(timeout)`、`SemaphoreSlim.Wait(timeout)`
- **不要在持锁时调用外部代码 / 回调 / await**

#### 4. 线程池是有限资源

- 线程池线程数受 `ThreadPool.SetMinThreads` 和注入速率限制，**突然的阻塞会触发饥饿**：队列堆积、延迟从毫秒级跳到秒级。
- 判断信号：`ThreadPool.PendingWorkItemCount` 持续不为 0、`dotnet-counters` 里 threadpool queue length 上涨。
- 药方：**别阻塞**（这是唯一根治），必要时调高 MinThreads 只是止血。

#### 5. 结构化并发

- `Task.WhenAll` → 全部完成（失败也要等全部结束再抛）
- `Task.WhenAny` → 超时 / 抢答模式
- **关联取消**：父操作取消 → 子操作全取消
- **别 fire-and-forget**：裸 `_ = DoAsync()` 会丢异常。要么 await，要么挂个 `ContinueWith` 记日志，要么用后台队列（`IHostedService` / Channel）

---

### 5.4 两者的交叉点（事故高发区）

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

#### 本仓库的具体落点（t1_frame）

- ABP 应用服务是 `async virtual Task`，**整条链（Controller → AppService → Repository → EF Core）都该是 async**，别在中间 `.Result` 掉。ABP 的拦截器（UoW、权限、审计）依赖 `AsyncLocal`，同步阻塞会破坏上下文流动。
- 迁移里有 `async virtual Task` 方法时用 `await _repo.GetListAsync()`（ABP 提供的异步仓储），不要 `GetList()` 同步版。
- 测试（NUnit）里 `async Task` 测试方法不要写成 `async void`，否则失败不会计入结果。

---

### 5.5 10 道自测题答案

#### 1. `await` 一个已完成的 Task，会切换线程吗？

**不会。**

编译器生成的 awaiter 先检查 `IsCompleted`；为 `true` 就直接从状态机继续往下执行，走的是同步快速路径：

```csharp
var t = Task.FromResult(42);
var v = await t;        // 完全同步执行，无 continuation 调度，无线程切换
```

只有 `IsCompleted == false` 时，才把自己挂成 continuation（注册回调）、`return` 出让线程，等 I/O 完成后由线程池恢复。

推论：`await` 本身不保证「异步」，也不保证「换线程」。想强制出让用 `await Task.Yield()`。

> 「让出线程」这个说法容易误解成「释放线程」或「UI 线程没让出」，见 [第六节 FAQ 2](#六追问与澄清faq)。

#### 2. 为什么 `Task.Result` 在老 ASP.NET 里死锁，在 ASP.NET Core 里只是「变慢」？

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

#### 3. `async void` 的异常去哪了？

**没有任何 Task 承载它，它被直接抛到捕获的上下文上，默认等于让进程崩掉。**

- 有 `SynchronizationContext` → 抛给该上下文 → 通常变成未处理异常
- 没有（线程池）→ 未处理异常直接终止进程
- `TaskScheduler.UnobservedTaskException` **收不到**（那是给被遗弃的 Task 用的）
- `try/catch` 只能写在 `async void` 方法**内部**才有效；调用方包 try/catch 完全没用
- 测试框架里 `async void` 测试会「提前通过」或直接崩 runner

这是它只允许出现在事件处理器（`button.Click += async (s, e) => ...`）的原因——那时代码语义就是「发了就不管」。

#### 4. 为什么 `lock` 里不能 `await`？

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

#### 5. `ConcurrentDictionary.GetOrAdd` 的工厂为什么不能有副作用？

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

#### 6. 异步方法里 `using` 的 `DbContext` 什么时候释放？

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

#### 7. 线程池饥饿的表现是什么，怎么定位？

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

#### 8. `Task.WhenAll` 里 3 个任务 2 个失败，`await` 会抛几次异常？

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

#### 9. CPU 密集任务加 `async` 有意义吗？

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

#### 10. `ValueTask` 为什么不能 await 两次？

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

### 5.6 追问与澄清（FAQ）

#### FAQ 1：能不能理解为「UI 线程不受线程池管控」，所以 `.Result` 才死锁？

**对了一半，但这不是成因。**

「UI 主线程不是线程池线程」这个事实本身成立——它是进程启动时创建的主线程，有自己的消息循环，`ThreadPool` 管不到它。但线程池既没调度它、也没阻塞它，所以「管辖关系」不是关键。

真正的因果是：

> **`await` 之后的续体被强制调度回「那条正在被阻塞的线程」执行。** `.Result` 把线程堵住，续体还必须回到这条线程 → 循环等待。

反证很直接：控制台程序也没有 `SynchronizationContext`，`.Result` 却不会死锁。**决定性因素是「有没有 `SynchronizationContext`」，而不是「线程归谁管」。**

##### `SynchronizationContext` 是续体调度策略，不是线程

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

##### 这个类比在老 ASP.NET 上已经不成立

老 ASP.NET **没有 UI 线程**，它是多线程的线程池处理模型，请求线程就来自 `ThreadPool`。它照样死锁，是因为 `AspNetSynchronizationContext` 要求「同一请求的续体串行执行，且需要原请求线程交还控制权」。把请求线程钉死，该请求的内部队列就永远等不到人来处理。

所以准确表述是：

- **WinForms / WPF**：单线程消息泵被阻塞，续体排不上队。
- **老 ASP.NET**：请求上下文被阻塞，续体排不上队。
- 共性是「续体绑在一条已阻塞的线程上」，不是「线程池管不到 UI 线程」。

##### ASP.NET Core 为什么「只是变慢」

`SynchronizationContext.Current == null`，续体走 `ThreadPool.QueueUserWorkItem`，任何空闲池线程都能跑，构不成循环等待。代价转移到了三处：

1. **阻塞的线程本可以服务其他请求**，可用并发度下降；
2. **线程池扩容很慢**：从 MinThreads 往上靠 hill-climbing，注入速率大约每秒 1–2 个线程量级，追不上请求涌入；
3. **排队级联**：延迟从 ms 到秒 → 客户端超时重试 → 请求量翻倍 → 更堵。

症状是「**CPU 打不满，但 QPS 撑不住**」。这是吞吐崩塌，不是卡顿。

##### 重要修正：「ASP.NET Core 不会死锁」是有条件的

条件就是「线程池还能再注入线程」。

极端情况下所有池线程都阻塞在 `Monitor.Wait` 等 Task，没有任何线程能跑续体——此时 **ASP.NET Core 照样卡死**，CPU 归零、进程活着但不响应。

| | 老 ASP.NET / WinForms / WPF | ASP.NET Core / Console |
| --- | --- | --- |
| `SynchronizationContext` | 有 | `null` |
| 续体去哪 | 特定的那条线程 | 任意池线程 |
| 阻塞那条线程 | 续体永远排不上 → 循环等待 | 续体照跑，只是少了一条线程 |
| 典型结果 | **确定性死锁**（必现） | **吞吐崩塌 / 线程池饥饿**（概率性，量大也可能卡死） |
| 修复 | 全链路 async，或至少 `ConfigureAwait(false)` | 全链路 async（别无他法） |

##### 两个常见误解

1. **「`ConfigureAwait(false)` 能修死锁」**——它只是不让续体回原线程，绕过了循环等待。续体里访问 UI 控件照样炸（`InvalidOperationException: 调用线程无法访问此对象`）。这是绕过，不是修复。
2. **「这是 ASP.NET 特有的问题」**——不是。xUnit 有 `MaxConcurrencySyncContext`，NUnit 等测试框架也会安装同步上下文，在里面 `.Result` 一样死给你看。**判断依据永远是：此刻 `SynchronizationContext.Current` 是不是 null，以及它把续体投递到哪。**

---

#### FAQ 2：`await` 是「让出线程」的手段，可 UI 线程不也让出去了吗？

**对，UI 线程确实也让出去了。** 准确的说法应该是：

> **`await` 让出的是「当前线程的执行权」——把它交还给它的调度者。**

UI 线程交还给消息循环，服务端线程交还给线程池。两边都让了，但**让的方式和收益完全不同**。

##### 机制上「让出」就是一次 `return`

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

##### 但两种「让出」性质不同

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

##### 由此产生的四个修正

**1. `await` 不一定让出。** 若 Task 已完成（缓存命中、`Task.FromResult`），`IsCompleted == true`，状态机压根不 `return`，一次线程切换都没有。「让出」是条件行为。

**2. 「等」不等于「让」。** UI 上最好用的对照组：

```csharp
await Task.Delay(3000);   // UI 线程让出 → 界面照样拖动、动画照跑
Thread.Sleep(3000);       // UI 线程被独占 → 界面冻结 3 秒，未响应
```

**3. UI 上的 `async` 不产生吞吐收益，只产生响应性收益。** 它让界面不卡，但**不会让桌面程序「同时算得更快」**——CPU 侧并发度仍是 1，想并行还得 `Task.Run` 到线程池。这正印证了第一节那句话：**异步 ≠ 并发**。

**4. 续体回到同一线程是债，不是福利。** 代价是所有续体排队走同一条消息队列、彼此串行；UI 线程忙时续体的执行时机不可控（可能延迟几十上百毫秒）。所以库里写代码才要 `ConfigureAwait(false)`——能不去挤那条独木桥就别挤。

##### 修正后的表述

> **`await` 是把当前线程的「执行权」交还给它的调度者（消息循环 / 线程池），等操作完成后再把后续代码作为续体重新排入调度队列。它从不创建线程，也不保证切换线程，更不保证「释放」资源——收益完全取决于「谁接收了这次让出」。**

---

### 5.7 只记三句话

1. **`await` 是「让出线程」的手段，不是「开线程」的手段。**
2. **所有并发 bug 都来自共享可变状态；`lock` 只解决同步代码的互斥，跨 await 用 `SemaphoreSlim`。**
3. **阻塞异步代码（`.Result` / `.Wait()` / `async void` / 跨 await 持锁）是这套体系里 90% 事故的来源。**

---

## 六、常考手写题

### 34. 线程安全单例

```csharp
public sealed class Singleton
{
    private static readonly Lazy<Singleton> _instance =
        new(() => new Singleton(), LazyThreadSafetyMode.ExecutionAndPublication);

    public static Singleton Instance => _instance.Value;

    private Singleton() { }
}
```

### 35. 两个线程交替打印奇偶数

```csharp
private static readonly object _lock = new();
private static int _number = 1;

static void Print(bool odd)
{
    while (true)
    {
        lock (_lock)
        {
            while (_number <= 100 && (_number % 2 == 1) != odd)
            {
                Monitor.Wait(_lock);
            }
            if (_number > 100) { Monitor.PulseAll(_lock); return; }
            Console.WriteLine(_number++);
            Monitor.PulseAll(_lock);
        }
    }
}
```

### 36. 生产者-消费者（Channel）

```csharp
var channel = Channel.CreateBounded<int>(100);

var producer = Task.Run(async () =>
{
    for (int i = 0; i < 1000; i++) await channel.Writer.WriteAsync(i);
    channel.Writer.Complete();
});

var consumer = Task.Run(async () =>
{
    await foreach (var item in channel.Reader.ReadAllAsync())
    {
        Console.WriteLine(item);
    }
});

await Task.WhenAll(producer, consumer);
```

### 37. 线程安全缓存

```csharp
public sealed class SafeCache<TKey, TValue> where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, Lazy<TValue>> _cache = new();

    public TValue GetOrAdd(TKey key, Func<TKey, TValue> factory)
        => _cache.GetOrAdd(key, k => new Lazy<TValue>(() => factory(k))).Value;
}
```

### 38. 统计字符出现次数

```csharp
var dict = text.GroupBy(c => c).ToDictionary(g => g.Key, g => g.Count());

// 或
var counts = new Dictionary<char, int>();
foreach (var c in text)
{
    counts[c] = counts.TryGetValue(c, out var n) ? n + 1 : 1;
}
```

### 39. 反转字符串 / 链表

```csharp
// 字符串
var reversed = new string(text.Reverse().ToArray());

// 链表
static ListNode? Reverse(ListNode? head)
{
    ListNode? prev = null;
    while (head != null)
    {
        var next = head.Next;
        head.Next = prev;
        prev = head;
        head = next;
    }
    return prev;
}
```

---

## 七、自测清单

打勾表示能**不看资料讲清原理并写出示例**：

- [ ] 值类型/引用类型、装箱拆箱
- [ ] struct vs class、可变 struct 的坑
- [ ] `==` vs `Equals` vs `GetHashCode`
- [ ] `virtual`/`override`/`new` 的输出题
- [ ] 闭包与循环变量捕获
- [ ] `yield return` 状态机
- [ ] GC 分代、LOH、Dispose 模式
- [ ] `IEnumerable` vs `IQueryable`、延迟执行
- [ ] `async/await` 状态机、`async void`、死锁成因
- [ ] 并发 / 并行 / 异步的边界，`await` 为什么不创建线程（§五 10 道自测）
- [ ] `lock`/`Monitor`/`Interlocked`/`volatile`
- [ ] 线程池饥饿的成因与避免
- [ ] 手写：单例、交替打印、Channel 生产者消费者

---

> 复习建议：先过「自测清单」，答不出的回到对应章节；面试前一天重点看**异步死锁**、**闭包捕获**、**GC 与内存泄漏**——这三块追问最多。

---

# 附录 A：15 分钟背诵卡

> 每条一句话，细节回正文对应题号。

### 类型与语法（1~14 题）

1. 值类型存数据本身（栈/内联），引用类型存堆地址；**赋值语义不同**，装箱是值→object 的堆拷贝。
2. struct：值语义、无继承、适合 ≤16 字节的小数据；**可变 struct 的坑**是属性修改可能落在副本上，用 `readonly struct`。
3. `==` 可重载（值比较或引用比较），`Equals` 是虚方法；**重写 Equals 必须重写 GetHashCode**，否则字典/哈希集合失效。
4. `const` 编译期常量且**内联进调用方**（改了要重编译引用方）；`readonly` 运行期赋值（字段初始化器/构造函数）。
5. `ref` 进出都可改、`out` 必须在方法内赋值、`in` 只读引用（大 struct 免拷贝）。
6. 抽象类 = 单继承 + 可带状态与实现；接口 = 多实现 + 契约，C# 8 起可有默认实现。
7. `new` 是**隐藏**（编译期按声明类型绑定），`override` 是**重写**（运行期多态）。
8. 委托是类型安全的函数指针（多播 = 调用链表）；事件是委托的封装，**外部只能 +=/-=**。
9. 泛型：编译期类型检查 + 免装箱 + 代码复用；`out` 协变（`IEnumerable<T>`）、`in` 逆变（`IComparer<T>`），**仅引用类型**。
10. 闭包捕获的是**变量不是值**（编译器提升为字段）；C# 5+ `foreach` 每次迭代新变量，`for` 仍共享同一个。
11. `yield return` 由编译器生成状态机，**延迟执行**，每次枚举从头跑一遍。
12. `using` 编译成 `try/finally` + `Dispose`；Dispose 模式要 `Dispose(bool)` + `GC.SuppressFinalize`。
13. `string` 不可变 → 线程安全 + 哈希可缓存 + 字面量驻留；大量拼接用 `StringBuilder`。
14. `Span<T>` 是栈上的零分配视图（`ref struct`），**不能跨 await、不能当字段**；堆上场景用 `Memory<T>`。

### 内存与 GC（15~18 题）

15. 分三代 + LOH（≥85KB）；标记-清除-压缩，0/1 代频繁、2 代贵；Server GC 每核一个堆。
16. 泄漏常见五处：静态事件订阅、未 Dispose 的 Timer/流、无上限缓存、闭包持有大对象、未观察的 Task。
17. `WeakReference` 做「可被回收的缓存」；`ConditionalWeakTable` 给对象挂附加数据。
18. `GC.SuppressFinalize` 避免已 Dispose 的对象再进终结队列（终结器会让对象多活一轮并晋升）。

### 集合与 LINQ（19~22 题）

19. `List` 随机访问、`Dictionary` O(1) 查找、`HashSet` 去重、`SortedSet/SortedDictionary` 有序、`Queue/Stack` 语义、`Concurrent*` 并发。
20. `IEnumerable` 是拉取式内存迭代，`IQueryable` 持有**表达式树**，由 Provider 翻译成 SQL。
21. 延迟执行三坑：**多次枚举重复执行**、闭包捕获变化、异常在枚举时才抛；要固化用 `ToList()`。
22. `First` 空集合抛异常、`FirstOrDefault` 返回默认值、`Single` 多于一条也抛。

### 并发与异步（23~33 题）

23. `async/await` = 状态机 + `AsyncTaskMethodBuilder`；**await 不创建线程**，只是挂起后续逻辑。
24. `async void` 无法等待、异常逃逸到同步上下文 → 只能用于事件处理器。
25. `.Result`/`.Wait()` 在有 `SynchronizationContext` 的环境里，线程池线程等 UI 线程 → **死锁**。
26. `Thread` 是 OS 线程（1:1、贵）；`Task` 是线程池上的工作单元（可组合、可取消）。
27. `lock` = `Monitor.Enter/Exit` + try/finally；锁对象不能是 `this`、`string`、值类型（装箱后每次都是新对象）。
28. 死锁四条件：互斥、持有并等待、不可剥夺、循环等待；破环靠**固定加锁顺序** + 超时。
29. `Interlocked` 是硬件 CAS 原子操作；`volatile` 只保证可见性/禁止重排、**不保证原子**；`lock` 两者都管。
30. `WhenAll` 等全部（异常只抛第一个）、`WhenAny` 等第一个完成（常用于超时）。
31. `CancellationToken` 是协作式取消：传递 → `ThrowIfCancellationRequested` → `CreateLinkedTokenSource` 组合。
32. 线程池饥饿 = 同步阻塞占满线程；解法是全链路异步 + 不用 `.Result` + 必要时 `SetMinThreads`。
33. `SemaphoreSlim` 进程内且支持 `WaitAsync`；`Semaphore` 可命名、跨进程。

> 记忆锚点：**类型 → 内存 → 集合 → 并发 → 手写题**。

---

# 附录 B：模拟面试 20 题（含追问链）

### 1. struct 和 class 怎么选？

**开口**：看语义不是看大小——值语义、小的、不可变的数据用 struct，其余用 class。
**追问**：什么时候 struct 反而更慢？→ 大于 16~24 字节、频繁装箱、放进数组后又大量拷贝时。

### 2. 为什么重写 `Equals` 必须重写 `GetHashCode`？

**开口**：哈希容器先按 hash 定位桶，再用 Equals 比较；hash 不一致就永远找不到那个对象。
**追问**：只重写 GetHashCode 行不行？→ 不行，可能出现「hash 相同但 Equals 判不等」，桶内退化成链表。

### 3. 闭包到底捕获了什么？

**开口**：捕获的是**变量**，编译器把它提升成编译器生成类的字段，方法变成该类的方法。
**追问**：`for` 循环里创建 10 个 lambda 会怎样？→ 全部捕获同一个 `i`，最后都是 10；要每次新建局部变量。

### 4. `yield return` 是怎么实现的？

**开口**：编译器生成状态机类，实现 `IEnumerable<T>/IEnumerator<T>`，用 `state` 字段记住执行到哪。
**追问**：`yield` 方法里能写 `try/catch` 吗？→ 可以有 `try/finally`，但**不能 yield 在 catch 块里**。

### 5. GC 什么时候触发 Full GC？

**开口**：2 代预算耗尽、LOH 分配压力大、显式 `GC.Collect()`（生产别调）、内存紧张。
**追问**：为什么 LOH 特别容易碎片？→ LOH 默认**不压缩**（只有 `GCSettings.LargeObjectHeapCompactionMode` 才压），大对象反复分配释放就留空洞。

### 6. 内存泄漏怎么排查？

**开口**：先看内存曲线是否只涨不落，再用 `dotnet-counters` 看 GC 堆，`dotnet-gcdump`/`dotnet-dump` 抓快照比对对象数量。
**追问**：最常见的泄漏源？→ 静态事件没退订、单例持有 Scoped 对象、无上限缓存、Timer 没 Dispose。

### 7. `async/await` 会创建线程吗？

**开口**：不会，await 只是把后续代码注册成回调并释放当前线程；真正的线程来自线程池。
**追问**：那 `Task.Run` 呢？→ 那才是显式把工作丢到线程池，CPU 密集型才需要。

### 8. 为什么 `.Result` 会死锁？

**开口**：同步阻塞等待时，`SynchronizationContext` 的线程被占住，而 await 的续体要回到那个线程 → 互相等。
**追问**：控制台程序为什么没事？→ 没有 `SynchronizationContext`，续体走线程池。

### 9. `ConfigureAwait(false)` 是什么意思？

**开口**：不要回到原上下文，续体直接在线程池执行；库代码该用，UI/请求上下文里要谨慎。
**追问**：ASP.NET Core 里还需要吗？→ 没有 `SynchronizationContext`，影响很小，但库代码仍建议加。

### 10. `async void` 为什么危险？

**开口**：调用方无法 await、异常无法被捕获，直接抛到同步上下文，通常等于进程崩溃。
**追问**：那事件处理器怎么办？→ 只能在最外层用，且内部必须自己 `try/catch` 兜住所有异常。

### 11. `lock` 有哪些坑？

**开口**：锁对象选错（this/string/值类型）、锁内做 IO 或 await、锁粒度过大。
**追问**：能用 `lock` 包 `await` 吗？→ 不能，编译不过；异步场景用 `SemaphoreSlim`。

### 12. `volatile` 能代替 `lock` 吗？

**开口**：不能，`volatile` 只保证可见性和禁止重排，`i++` 依然不是原子操作。
**追问**：那什么时候够用？→ 单个标志位（`bool _stop`）的读写，且没有复合操作。

### 13. `Interlocked` 是怎么做到原子的？

**开口**：底层是 CPU 的 CAS 指令 + 内存屏障，无锁循环重试。
**追问**：`Interlocked.Increment` 和 `lock` 性能差多少？→ 无竞争时快一个数量级；高竞争时 CAS 空转，反而不如分段计数。

### 14. 线程池饥饿怎么出现的？

**开口**：大量同步阻塞（`.Result`、`Thread.Sleep`、同步 IO）把线程池线程占满，新任务排不上队。
**追问**：为什么加了线程池上限反而更糟？→ 排队变长、延迟抖动；正解是全链路异步。

### 15. `Task` 和 `Thread` 的本质区别？

**开口**：Thread 是 OS 线程，Task 是「工作单元 + 状态机 + 调度」，可以没有专属线程。
**追问**：`Task.Run` 一定开新线程吗？→ 不一定，线程池有闲置线程就直接复用。

### 16. `CancellationToken` 怎么正确传递？

**开口**：每层都接收并透传，阻塞/循环里 `ThrowIfCancellationRequested`，组合用 `CreateLinkedTokenSource`。
**追问**：取消后资源怎么清理？→ 用 `try/finally` 或 `CancellationToken.Register` 注册回调。

### 17. `IEnumerable` 延迟执行踩过什么坑？

**开口**：一次查询被枚举两次 → 查两遍库；闭包变量变化导致结果不同；异常推迟到枚举时才爆。
**追问**：怎么避免？→ 明确边界处 `ToList()` 固化，或者直接用 `IQueryable` 到最后一刻再执行。

### 18. `IQueryable` 为什么能把代码翻译成 SQL？

**开口**：它存的是**表达式树**而不是委托，Provider 遍历树生成 SQL。
**追问**：哪些写法翻译不了？→ 自定义方法、部分字符串操作、`DateTime` 的某些属性，会退化成客户端求值或抛异常。

### 19. `Span<T>` 为什么不能跨 `await`？

**开口**：`ref struct` 只能活在栈上，await 会把状态机搬到堆上，编译器直接禁止。
**追问**：那异步里怎么做零分配切片？→ 用 `Memory<T>`，它不含托管指针，可以放进堆。

### 20. 手写一个线程安全单例，并说说为什么这么写。

**开口**：`Lazy<T>`（`LazyThreadSafetyMode.ExecutionAndPublication`）最省心；手写用 `static readonly` + 静态构造函数，由 CLR 保证线程安全。
**追问**：双重检查锁在 C# 里还要 `volatile` 吗？→ 要，防止「先发布引用、后完成构造」的重排——这也是为什么推荐 `Lazy<T>`。

---

# 附录 C：口述版——3 分钟讲清 C# 与并发

> 面试官问「你最熟 C# 的哪部分」时用。约 800 字 ≈ 3 分钟。

### 逐字稿

**【开场 · 15 秒】**
我理解的 C# 有两个关键词：**类型安全**和**托管运行时**。语言层面它把很多容易出错的东西交给编译器和 GC，比如值/引用语义、`using`、`async/await`。所以我讲 C# 一般分三层：类型与内存、并发、工程实践。

**【类型与内存 · 45 秒】**
类型这块我会强调三件事。一是值类型和引用类型的**赋值语义**不同，装箱是隐式的堆分配，性能敏感的地方要避免。二是 `struct` 不是「小的 class」，它是值语义，可变 struct 改的可能是副本。三是 `string` 不可变，所以大量拼接必须用 `StringBuilder`；需要零分配切片就用 `Span<T>`，但它是 `ref struct`，不能跨 await。
内存这块是**分代 GC**：0/1 代频繁、2 代贵，大对象直接进 LOH 而且默认不压缩，所以大对象反复分配最容易碎片。确定性释放靠 `IDisposable` 和 `using`，Dispose 模式里要 `SuppressFinalize`。

**【并发 · 50 秒】**
并发是追问最密集的地方。`async/await` 本质是**编译器生成状态机**，await 不创建线程，只是把后续逻辑注册成回调、把当前线程还回去。
最容易出错的是上下文：`.Result` 和 `.Wait()` 在有 `SynchronizationContext` 的环境里会死锁——线程被同步等待占住，续体回不到那个线程。所以要么全链路异步，要么库代码 `ConfigureAwait(false)`。
线程安全三件套我这么分工：`lock` 管互斥和内存屏障、`Interlocked` 管单个变量的原子操作、`volatile` 只管可见性。线程池饥饿基本都是同步阻塞造成的，不是池子小。

**【工程实践 · 40 秒】**
工程上我会盯三个点。LINQ 的**延迟执行**：`IEnumerable` 每次枚举都重跑，边界处要 `ToList()`；`IQueryable` 是表达式树，能翻译成 SQL，写错就退化。EF Core 的**跟踪查询**会占内存，只读场景上 `AsNoTracking`。并发共享状态尽量用不可变对象或 `Channel`/`ConcurrentDictionary`，而不是到处加锁。

**【收尾 · 20 秒】**
如果让我评估一段 C# 代码，我会看三件事：**分配多不多、阻塞有没有、出问题能不能查**。前两个决定性能，最后一个决定它能不能在生产上活下来。

---

### 压缩版

**一句话版（10 秒）**
> C# 的功夫在类型语义、分代 GC 和异步状态机；踩坑最多的是闭包捕获、`lock` 选错对象、`.Result` 死锁。

**30 秒版**
> 类型上注意值/引用语义和装箱，GC 是分代 + LOH 不压缩，确定性释放靠 IDisposable。异步是状态机，await 不开线程，`.Result` 会死锁。线程安全用 lock/Interlocked/volatile 分工，线程池饥饿来自同步阻塞。工程上注意 LINQ 延迟执行和 EF 跟踪查询。

### 时间分配

| 段落 | 时长 | 只说一件事 |
| --- | --- | --- |
| 开场 | 15s | 类型安全 + 托管运行时，分三层讲 |
| 类型与内存 | 45s | 赋值语义 → 可变 struct → Span/GC/Dispose |
| 并发 | 50s | 状态机 → 死锁 → 三件套分工 → 线程池饥饿 |
| 工程实践 | 40s | 延迟执行、跟踪查询、少加锁 |
| 收尾 | 20s | 分配 / 阻塞 / 可观测 |

### 被打断时怎么接

| 面试官插话 | 接续句 |
| --- | --- |
| 「说说 GC 原理」 | 「这就是我下面要说的分代——」 |
| 「为什么会有死锁？」 | 「根因是 SynchronizationContext，我举个 UI 线程的例子——」 |
| 「锁和 Interlocked 怎么选」 | 「看是不是复合操作：单个计数用 Interlocked，多字段不变式用 lock」 |
| 「你们线上踩过什么坑」 | 「闭包捕获循环变量导致全批订单指向最后一条，改法是循环内建局部变量」 |
