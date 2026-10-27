# .NET 经典面试题（含要点与代码）

> 面向 C# / ASP.NET Core 后端岗位的常见面试题整理。按“出现频率 + 易追问点”组织，每题给出**要点答案**与**常见追问**。
> 建议用法：先遮住答案自测，答不出的题目标记后重点复习。

## 目录

- [一、C# 语言基础](#一c-语言基础)
- [二、内存管理与 GC](#二内存管理与-gc)
- [三、集合与 LINQ](#三集合与-linq)
- [四、多线程与异步](#四多线程与异步)
- [五、ASP.NET Core](#五aspnet-core)
- [六、EF Core](#六ef-core)
- [七、分布式与架构](#七分布式与架构)
- [八、常考手写题](#八常考手写题)
- [九、经典业务场景题（电商）](#九经典业务场景题电商)
- [十、自测清单](#十自测清单)

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

## 五、ASP.NET Core

### 34. 请求管道和中间件？

中间件按注册顺序组成管道，形成“洋葱模型”：`next()` 之前是请求处理，之后是响应处理。

典型顺序：

```
异常处理 → HSTS/HTTPS 重定向 → 静态文件 → 路由 → CORS → 认证 → 授权 → 端点
```

> 追问：`Use`（可调 next）、`Run`（终端，不调 next）、`Map` / `MapWhen`（分支）。

---

### 35. DI 的三种生命周期？

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

### 36. 过滤器有哪些？执行顺序？

授权 → 资源 → 动作 → 异常 → 结果。

- `IAuthorizationFilter`
- `IResourceFilter`（可短路，缓存常用）
- `IActionFilter` / `IAsyncActionFilter`
- `IExceptionFilter`
- `IResultFilter`

---

### 37. 认证和授权怎么做的？

- 认证（你是谁）：JWT Bearer、Cookie、OIDC。
- 授权（你能做什么）：`[Authorize]`、策略 `AddPolicy`、基于资源/声明的授权、自定义 `IAuthorizationHandler`。

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { /* TokenValidationParameters */ });

builder.Services.AddAuthorization(o =>
    o.AddPolicy("AdminOnly", p => p.RequireRole("Admin")));
```

---

### 38. 配置系统的优先级？

`appsettings.json` → `appsettings.{Environment}.json` → 用户机密（开发）→ 环境变量 → 命令行参数。**后加载覆盖先加载**。

---

### 39. 如何提升 API 性能？

- 全链路异步，避免同步阻塞。
- EF Core 只读查询 `AsNoTracking()`，分页，避免 N+1。
- 多级缓存（内存 + Redis）、响应缓存、输出压缩。
- 连接池（`HttpClientFactory`、`DbContextPool`）。
- 流式处理大响应（`IAsyncEnumerable` + `Response.BodyWriter`）。
- 限流、熔断、降级。

---

### 40. `IHttpContextAccessor` 为什么默认不注册？

`HttpContext` 不是线程安全的，且后台任务中不存在请求上下文。需要时显式 `AddHttpContextAccessor()`，并在后台任务中新建 scope。优先通过方法参数传递所需数据，而不是到处取 `HttpContext`。

---

## 六、EF Core

### 41. `DbContext` 的生命周期？线程安全吗？

Scoped（每请求一个）。**非线程安全**，不能并行使用同一实例；后台任务要新建 scope。

```csharp
using var scope = serviceProvider.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
```

---

### 42. 跟踪查询 vs 无跟踪？

默认跟踪会生成快照用于变更检测，只读查询用 `AsNoTracking()` 提升性能、减少内存。

---

### 43. 怎么避免 N+1？

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

### 44. 并发冲突怎么处理？

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

### 45. 迁移（Migrations）流程？

```powershell
dotnet ef migrations add AddUserTable --project src/.../xxx.entityframeworkcore
dotnet ef database update --project src/.../xxx.entityframeworkcore
```

生产环境推荐 `dotnet ef migrations script` 生成 SQL 脚本，评审后执行，避免应用启动时自动迁移。

---

## 七、分布式与架构

### 46. 缓存穿透、击穿、雪崩？

- **穿透**：查询不存在的数据，每次都打到 DB → 布隆过滤器 / 缓存空值。
- **击穿**：热点 key 过期瞬间大量请求 → 互斥锁重建 / 逻辑过期。
- **雪崩**：大量 key 同时过期或缓存宕机 → 随机过期时间、多级缓存、熔断降级。

---

### 47. 如何保证接口幂等？

- 唯一业务号 + 数据库唯一索引。
- Redis `SET key value NX PX` 去重。
- 状态机校验（只允许从“待支付”变为“已支付”）。
- 消息消费端去重表（消息 ID）。

---

### 48. 分布式锁怎么做？

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

### 49. CAP 与最终一致性？

分布式事务优先**避免**：本地消息表、事务消息、Saga、TCC。跨服务追求最终一致 + 补偿 + 幂等。

---

### 50. 如何设计一个高并发接口？

- 分层削峰：CDN → 网关限流 → 应用层缓存 → 队列异步化 → DB 读写分离/分库分表。
- 无状态服务 + 水平扩容。
- 热点数据隔离，避免单点。
- 降级预案与压测（明确 QPS / P99 目标）。

---

## 八、常考手写题

### 1. 线程安全单例

```csharp
public sealed class Singleton
{
    private static readonly Lazy<Singleton> _instance =
        new(() => new Singleton(), LazyThreadSafetyMode.ExecutionAndPublication);

    public static Singleton Instance => _instance.Value;

    private Singleton() { }
}
```

### 2. 两个线程交替打印奇偶数

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

### 3. 生产者-消费者（Channel）

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

### 4. 线程安全缓存

```csharp
public sealed class SafeCache<TKey, TValue> where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, Lazy<TValue>> _cache = new();

    public TValue GetOrAdd(TKey key, Func<TKey, TValue> factory)
        => _cache.GetOrAdd(key, k => new Lazy<TValue>(() => factory(k))).Value;
}
```

### 5. 统计字符出现次数

```csharp
var dict = text.GroupBy(c => c).ToDictionary(g => g.Key, g => g.Count());

// 或
var counts = new Dictionary<char, int>();
foreach (var c in text)
{
    counts[c] = counts.TryGetValue(c, out var n) ? n + 1 : 1;
}
```

### 6. 反转字符串 / 链表

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

## 九、经典业务场景题（电商）

> 这一章是“八股之外的硬仗”。面试官问业务场景时，真正想听的是：**你知不知道并发下会出什么问题、方案之间怎么取舍、失败后怎么补偿**。
> 回答套路：`场景 → 问题 → 方案演进 → 取舍 → 一致性/幂等/补偿`。
>
> 配套可运行示例：`t1_frame/src/application/t1_frame.application.abp/Inventory`，
> 接入现有 MySQL + Redis + RabbitMQ/Kafka + MongoDB，启动 `t1_frame.webapi.abp` 后访问
> `/api/app/inventory/...`，提供 naive / atomic / optimistic / redis / idempotent 五种扣减方式
> 和一个并发压测接口。配套 Vue 前端在 `t1_frame/frontend/inventory-web`
> （商品商店 / 库存管理 / 订单管理 / 审计日志），支持下单、支付、库存扣减、11 套换肤与中英双语。
> 另外实现了 Redis 分桶 + 批量落库的 1000 QPS 秒杀通道，见 `docs/seckill-1000qps.md`。
> 运行说明与实测结果见 `docs/inventory-deduction.md`。

### 1. 库存扣减：超卖是怎么产生的？

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

### 2. 库存扣减方案对比

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

### 3. 方案一：数据库原子扣减（标准答案）

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

### 4. 方案二：乐观锁

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

### 5. 方案三：悲观锁

```sql
BEGIN;
SELECT stock FROM t_product WHERE id = #{id} FOR UPDATE;
-- 应用层判断 stock >= n
UPDATE t_product SET stock = stock - #{n} WHERE id = #{id};
COMMIT;
```

注意：必须走主键/索引，否则可能锁范围过大；事务内不要做 RPC；高并发会排队，QPS 上不去。

---

### 6. 方案四：Redis 预扣 + Lua（秒杀核心）

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

### 7. 方案五：分段库存（超级热点）

一个商品 10 万库存，单 key 或单行必然成为瓶颈。拆成 100 个桶，每桶 1000：

```
stock:1001:0 ... stock:1001:99
```

- 请求按 `userId % 100` 路由到桶，桶空则顺延尝试其他桶。
- 优点：并发度提升 N 倍；缺点：实现复杂，要处理“总库存汇总”和“桶间不均衡”。
- 通常配合 Redis 预扣使用，DB 侧仍按总量做最终校验。

---

### 8. 完整下单链路（可以直接背）

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

### 9. 库存回补与少卖

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

### 10. 幂等设计的四种武器

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

### 11. 缓存与数据库一致性

- 常用：**先更新 DB，再删除缓存**；读时回填。
- 延迟双删：更新后删一次，延迟几百毫秒再删一次，降低并发脏读窗口。
- 更强：订阅 binlog（Canal / Debezium）异步删缓存。
- 库存场景：**DB 是唯一真相**，缓存只做加速，允许短暂不一致，靠对账收敛。

> 不要用“先删缓存再更新 DB”，并发读会把旧值回填；也不要给缓存设永不过期还指望强一致。

---

### 12. 优惠券领取（超发问题）

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

### 13. 账户余额扣减

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

### 14. 订单超时关闭怎么做？

| 方案 | 原理 | 优缺点 |
| --- | --- | --- |
| 定时扫表 | 每分钟扫“待支付且超时” | 简单，但有延迟、大表压力大 |
| RabbitMQ TTL + 死信 | 消息过期进死信队列 | 精度受队列头阻塞影响 |
| RocketMQ 延迟消息 | 指定延迟级别 | 精度较好，依赖中间件 |
| Redis ZSet | score = 到期时间，轮询 | 轻量，需自己保证可靠 |
| 时间轮 | 内存定时器 | 性能好，重启需持久化 |

无论哪种，**关单和回补都要幂等**，并且要防止“关单”和“支付成功”并发——用状态机 CAS 决出胜负。

---

### 15. 秒杀系统分层设计

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

### 16. 高频追问清单

- **为什么不用分布式锁扣库存？** 性能差、锁超时与续期复杂、Redis 主从切换可能丢锁；DB 原子更新本身就能保证不超卖。
- **Redis 扣成功、DB 扣失败怎么办？** 回补 Redis + 重试 + 对账；接受最终一致，不能接受“扣了钱没货”。
- **MQ 消息丢了怎么办？** 本地消息表（业务与消息同事务）+ 发送确认 + 定时补偿 + 死信人工处理。
- **如何保证消息不被重复消费？** 业务唯一号 + 消费去重表/Redis + 幂等状态机。
- **大促前怎么准备？** 容量评估、热点打散、缓存预热、限流阈值、降级预案、全链路压测、监控告警。
- **怎么证明没有超卖？** 对账 SQL：`初始库存 = 剩余库存 + 已支付数量 + 锁定数量`，定时校验并告警。

---

## 十、自测清单

打勾表示能**不看资料讲清原理并写出示例**：

- [ ] 值类型/引用类型、装箱拆箱
- [ ] struct vs class、可变 struct 的坑
- [ ] `==` vs `Equals` vs `GetHashCode`
- [ ] `virtual`/`override`/`new` 的输出题
- [ ] 闭包与循环变量捕获
- [ ] GC 分代、LOH、Dispose 模式
- [ ] `IEnumerable` vs `IQueryable`、延迟执行
- [ ] `async/await` 状态机、`async void`、死锁成因
- [ ] `lock`/`Monitor`/`Interlocked`/`volatile`
- [ ] 中间件顺序与 DI 生命周期
- [ ] EF Core 跟踪、N+1、并发冲突
- [ ] 缓存三兄弟、幂等、分布式锁
- [ ] 库存扣减的 5 种方案及取舍
- [ ] 超卖 / 少卖 / 重复扣减的成因与修复
- [ ] 下单链路、订单状态机、超时关单
- [ ] Redis 预扣的补偿与对账设计

---

> 复习建议：先过“自测清单”，答不出的回到对应章节；面试前一天重点看**异步死锁**、**DI 生命周期**、**GC/内存泄漏**——这三块追问最多。
