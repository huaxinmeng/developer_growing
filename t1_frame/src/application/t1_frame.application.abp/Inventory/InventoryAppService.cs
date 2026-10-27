using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using StackExchange.Redis;
using t1_frame.entityframeworkcore.abp;
using t1_frame.response.abp;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Uow;

namespace t1_frame.application.abp
{
    /// <summary>
    /// 库存扣减示例：
    /// - MySQL：库存、流水、订单，条件更新 / 乐观锁 / 唯一索引；
    /// - Redis：Lua 预扣 + 失败回补，SETNX 幂等；
    /// - RabbitMQ / Kafka：扣减成功后发布 StockDeductedEto；
    /// - MongoDB：消费者写审计日志。
    /// </summary>
    public class InventoryAppService : ApplicationService, IInventoryAppService
    {
        private const string DemoProductName = InventorySchema.DemoProductName;

        // 返回值：-1 = key 不存在，-2 = 库存不足，1 = 扣减成功。
        // 注意不能把 DECRBY 的返回值当成功/失败标记：扣到最后一件时它返回 0。
        private const string DeductLua = @"
local stock = redis.call('GET', KEYS[1])
if not stock then
    return -1
end
if tonumber(stock) < tonumber(ARGV[1]) then
    return -2
end
redis.call('DECRBY', KEYS[1], ARGV[1])
return 1";

        private readonly IDbContextProvider<T1FrameAbpDbContext> _dbContextProvider;
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly IConnectionMultiplexer _redis;
        private readonly IDistributedEventBus _distributedEventBus;
        private readonly IMongoClient _mongoClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<InventoryAppService> _logger;

        public InventoryAppService(
            IDbContextProvider<T1FrameAbpDbContext> dbContextProvider,
            IUnitOfWorkManager unitOfWorkManager,
            IConnectionMultiplexer redis,
            IDistributedEventBus distributedEventBus,
            IMongoClient mongoClient,
            IConfiguration configuration,
            ILogger<InventoryAppService> logger)
        {
            _dbContextProvider = dbContextProvider;
            _unitOfWorkManager = unitOfWorkManager;
            _redis = redis;
            _distributedEventBus = distributedEventBus;
            _mongoClient = mongoClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<InventoryProductDto> GetAsync()
        {
            using var uow = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: false);
            var db = await _dbContextProvider.GetDbContextAsync();

            var product = await GetOrCreateProductAsync(db);
            var detail = await db.T1InventoryProductDetail
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == product.Id);

            await uow.CompleteAsync();
            return ToDto(product, detail);
        }

        public async Task<InventoryProductDto> ResetAsync(int stock = 100)
        {
            if (stock < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(stock), "库存不能为负数");
            }

            using var uow = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: true);
            var db = await _dbContextProvider.GetDbContextAsync();

            var product = await GetOrCreateProductAsync(db);

            await db.T1InventoryStockFlow
                .Where(x => x.product_id == product.Id)
                .ExecuteDeleteAsync();

            await db.T1InventoryOrder
                .Where(x => x.product_id == product.Id)
                .ExecuteDeleteAsync();

            product.goods_stock = stock;
            await db.SaveChangesAsync();
            await uow.CompleteAsync();

            await TrySetRedisStockAsync(product.Id, stock);

            var detail = await db.T1InventoryProductDetail
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == product.Id);

            return ToDto(product, detail);
        }

        /// <summary>
        /// 错误示范：读 → 内存计算 → 写回。
        /// 这里用 Task.Delay 放大竞态窗口，稳定复现丢失更新。
        /// </summary>
        public async Task<InventoryDeductResultDto> DeductNaiveAsync(int quantity = 1)
        {
            EnsureQuantity(quantity);

            using var uow = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: false);
            var db = await _dbContextProvider.GetDbContextAsync();

            var product = await GetOrCreateProductAsync(db);

            var snapshot = await db.T1GoodsStock
                .AsNoTracking()
                .Where(x => x.Id == product.Id)
                .Select(x => new { x.goods_stock })
                .FirstAsync();

            if (snapshot.goods_stock < quantity)
            {
                await uow.CompleteAsync();
                return Fail("库存不足", snapshot.goods_stock);
            }

            var newStock = snapshot.goods_stock - quantity;

            // 模拟真实业务里的校验、RPC、计算耗时
            await Task.Delay(30);

            // 错误点：把“我读到的库存 - n”直接写回，后写覆盖前写
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE `t1_goods_stock` SET `goods_stock` = {newStock} WHERE `Id` = {product.Id}");

            var remaining = await ReadStockAsync(db, product.Id);
            await uow.CompleteAsync();

            return new InventoryDeductResultDto
            {
                Success = true,
                Message = "扣减完成（错误示范：可能发生丢失更新）",
                RemainingStock = remaining,
                Attempts = 1,
                Source = "naive"
            };
        }

        /// <summary>
        /// 推荐方案：一条 SQL 完成“判断 + 扣减”，并校验受影响行数。
        /// </summary>
        public async Task<InventoryDeductResultDto> DeductAtomicAsync(int quantity = 1)
        {
            EnsureQuantity(quantity);

            using var uow = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: true);
            var db = await _dbContextProvider.GetDbContextAsync();

            var product = await GetOrCreateProductAsync(db);

            var affected = await db.T1GoodsStock
                .Where(x => x.Id == product.Id && x.goods_stock >= quantity)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.goods_stock, x => x.goods_stock - quantity));

            if (affected == 0)
            {
                var current = await ReadStockAsync(db, product.Id);
                await uow.RollbackAsync();
                return Fail("库存不足（或商品不存在）", current);
            }

            var bizNo = Guid.NewGuid().ToString("N");
            var remaining = await ReadStockAsync(db, product.Id);

            await db.T1InventoryStockFlow.AddAsync(NewFlow(product, bizNo, quantity, remaining, "atomic"));
            await db.T1InventoryOrder.AddAsync(NewOrder(bizNo, product.Id, quantity));
            await db.SaveChangesAsync();

            await uow.CompleteAsync();

            var published = await PublishDeductionEventAsync(
                bizNo, product, quantity, remaining, "atomic");

            return new InventoryDeductResultDto
            {
                Success = true,
                Message = "扣减成功（MySQL 条件更新，原子操作）",
                RemainingStock = remaining,
                Attempts = 1,
                BizNo = bizNo,
                Source = "atomic",
                EventPublished = published
            };
        }

        /// <summary>
        /// 乐观锁：用库存快照做 CAS，冲突后重新读取再试。
        /// </summary>
        public async Task<InventoryDeductResultDto> DeductOptimisticAsync(int quantity = 1)
        {
            EnsureQuantity(quantity);

            const int maxAttempts = 10;

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                // 关键：每次重试都开启新事务。
                // MySQL 默认 REPEATABLE READ，同一个事务里重试会一直读到同一份快照。
                using var uow = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: true);
                var db = await _dbContextProvider.GetDbContextAsync();

                var product = await GetOrCreateProductAsync(db);

                var snapshot = await db.T1GoodsStock
                    .AsNoTracking()
                    .Where(x => x.Id == product.Id)
                    .Select(x => new { x.goods_stock })
                    .FirstAsync();

                if (snapshot.goods_stock < quantity)
                {
                    await uow.RollbackAsync();
                    return Fail("库存不足", snapshot.goods_stock, "optimistic");
                }

                var affected = await db.T1GoodsStock
                    .Where(x => x.Id == product.Id
                                && x.goods_stock == snapshot.goods_stock
                                && x.goods_stock >= quantity)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(x => x.goods_stock, x => x.goods_stock - quantity));

                if (affected == 1)
                {
                    var bizNo = Guid.NewGuid().ToString("N");
                    var remaining = await ReadStockAsync(db, product.Id);

                    await db.T1InventoryStockFlow.AddAsync(NewFlow(product, bizNo, quantity, remaining, "optimistic"));
                    await db.T1InventoryOrder.AddAsync(NewOrder(bizNo, product.Id, quantity));
                    await db.SaveChangesAsync();

                    await uow.CompleteAsync();

                    var published = await PublishDeductionEventAsync(
                        bizNo, product, quantity, remaining, "optimistic");

                    return new InventoryDeductResultDto
                    {
                        Success = true,
                        Message = $"扣减成功（乐观锁，第 {attempt} 次尝试）",
                        RemainingStock = remaining,
                        Attempts = attempt,
                        BizNo = bizNo,
                        Source = "optimistic",
                        EventPublished = published
                    };
                }

                // 快照已被其他事务修改：回滚后重新开事务重试
                await uow.RollbackAsync();
                await Task.Delay(Random.Shared.Next(1, 5));
            }

            var currentStock = await ReadStockInNewUowAsync();

            return new InventoryDeductResultDto
            {
                Success = false,
                Message = $"并发冲突，重试 {maxAttempts} 次仍失败",
                RemainingStock = currentStock,
                Attempts = maxAttempts,
                Source = "optimistic"
            };
        }

        /// <summary>
        /// Redis Lua 预扣 → MySQL 条件更新 → 失败回补 Redis。
        /// </summary>
        public async Task<InventoryDeductResultDto> DeductWithRedisAsync(int quantity = 1)
        {
            EnsureQuantity(quantity);

            using var uow = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: true);
            var db = await _dbContextProvider.GetDbContextAsync();

            var product = await GetOrCreateProductAsync(db);
            var key = StockKey(product.Id);

            IDatabase redis;
            long redisResult;
            try
            {
                redis = _redis.GetDatabase();

                // 第一次访问时把 MySQL 库存预热到 Redis
                await redis.StringSetAsync(key, product.goods_stock, when: When.NotExists);

                redisResult = (long)await redis.ScriptEvaluateAsync(
                    DeductLua,
                    new RedisKey[] { key },
                    new RedisValue[] { quantity });

                if (redisResult == -1)
                {
                    // key 被删除或 Redis 刚重启：重新预热后再试一次
                    await redis.StringSetAsync(key, product.goods_stock);
                    redisResult = (long)await redis.ScriptEvaluateAsync(
                        DeductLua,
                        new RedisKey[] { key },
                        new RedisValue[] { quantity });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis 扣减失败，回退到数据库条件更新");

                // Redis 不可用时降级为数据库条件扣减，保证业务可用
                await uow.RollbackAsync();
                return await DeductAtomicAsync(quantity);
            }

            if (redisResult == -2)
            {
                var current = await ReadStockAsync(db, product.Id);
                await uow.RollbackAsync();
                return Fail("Redis 库存不足", current, "redis");
            }

            if (redisResult != 1)
            {
                await uow.RollbackAsync();
                return Fail("Redis 库存未初始化", await ReadStockAsync(db, product.Id), "redis");
            }

            // Redis 预扣成功后落库；任何失败都必须回补 Redis，避免少卖
            var needCompensation = true;
            try
            {
                // Redis 预扣成功说明 Redis 认为还有库存；数据库偶发锁冲突时重试几次
                var affected = 0;
                for (var attempt = 0; attempt < 3 && affected == 0; attempt++)
                {
                    affected = await db.T1GoodsStock
                        .Where(x => x.Id == product.Id && x.goods_stock >= quantity)
                        .ExecuteUpdateAsync(setters => setters
                            .SetProperty(x => x.goods_stock, x => x.goods_stock - quantity));

                    if (affected == 0)
                    {
                        await Task.Delay(5);
                    }
                }

                if (affected == 0)
                {
                    var current = await ReadStockAsync(db, product.Id);
                    _logger.LogWarning(
                        "Redis 预扣成功但数据库扣减失败：productId={ProductId}, dbStock={DbStock}, redisResult={RedisResult}",
                        product.Id, current, redisResult);

                    await uow.RollbackAsync();
                    return Fail("数据库库存不足，已回补 Redis", current, "redis");
                }

                var bizNo = Guid.NewGuid().ToString("N");
                var remaining = await ReadStockAsync(db, product.Id);

                await db.T1InventoryStockFlow.AddAsync(NewFlow(product, bizNo, quantity, remaining, "redis"));
                await db.T1InventoryOrder.AddAsync(NewOrder(bizNo, product.Id, quantity));
                await db.SaveChangesAsync();

                await uow.CompleteAsync();

                // 数据库已提交，Redis 的预扣保留
                needCompensation = false;

                var published = await PublishDeductionEventAsync(
                    bizNo, product, quantity, remaining, "redis");

                return new InventoryDeductResultDto
                {
                    Success = true,
                    Message = "扣减成功（Redis Lua 预扣 + MySQL 落库）",
                    RemainingStock = remaining,
                    Attempts = 1,
                    BizNo = bizNo,
                    Source = "redis",
                    EventPublished = published
                };
            }
            finally
            {
                if (needCompensation)
                {
                    try
                    {
                        await redis.StringIncrementAsync(key, quantity);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Redis 回补失败，key={Key}, quantity={Quantity}", key, quantity);
                    }
                }
            }
        }

        /// <summary>
        /// 幂等扣减：Redis SETNX 快速挡重复 + MySQL biz_no 唯一索引兜底。
        /// </summary>
        public async Task<InventoryDeductResultDto> DeductIdempotentAsync(string bizNo, int quantity = 1)
        {
            EnsureQuantity(quantity);

            if (string.IsNullOrWhiteSpace(bizNo))
            {
                throw new ArgumentException("bizNo 不能为空", nameof(bizNo));
            }

            using var uow = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: true);
            var db = await _dbContextProvider.GetDbContextAsync();

            var product = await GetOrCreateProductAsync(db);

            // 1. 数据库已经处理过：直接返回
            var existing = await db.T1InventoryStockFlow
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.biz_no == bizNo);

            if (existing != null)
            {
                await uow.CompleteAsync();
                return new InventoryDeductResultDto
                {
                    Success = true,
                    Message = "幂等命中：该业务号已扣减，未重复扣",
                    RemainingStock = existing.remaining_stock,
                    Attempts = 0,
                    BizNo = bizNo,
                    Source = existing.source
                };
            }

            // 2. Redis SETNX 挡住并发重复请求（Redis 异常时仍由唯一索引兜底）
            var idemKey = IdempotentKey(bizNo);
            var redisAcquired = true;
            try
            {
                redisAcquired = await _redis.GetDatabase().StringSetAsync(
                    idemKey, "1", TimeSpan.FromMinutes(10), When.NotExists);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis 幂等键写入失败，继续依赖数据库唯一索引");
            }

            if (!redisAcquired)
            {
                await uow.RollbackAsync();
                return new InventoryDeductResultDto
                {
                    Success = false,
                    Message = "请求处理中，请勿重复提交",
                    Attempts = 0,
                    BizNo = bizNo,
                    Source = "idempotent"
                };
            }

            var affected = await db.T1GoodsStock
                .Where(x => x.Id == product.Id && x.goods_stock >= quantity)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.goods_stock, x => x.goods_stock - quantity));

            if (affected == 0)
            {
                await TryDeleteIdempotentKeyAsync(idemKey);

                var current = await ReadStockAsync(db, product.Id);
                await uow.RollbackAsync();
                return Fail("库存不足（或商品不存在）", current, "idempotent");
            }

            var remaining = await ReadStockAsync(db, product.Id);

            await db.T1InventoryStockFlow.AddAsync(NewFlow(product, bizNo, quantity, remaining, "idempotent"));
            await db.T1InventoryOrder.AddAsync(NewOrder(bizNo, product.Id, quantity));

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // 3. 并发下同一 bizNo 同时插入：唯一索引让后到者失败，回滚本次扣减
                await uow.RollbackAsync();

                var current = await ReadStockInNewUowAsync();
                return new InventoryDeductResultDto
                {
                    Success = true,
                    Message = "重复请求（唯一索引兜底），本次扣减已回滚",
                    RemainingStock = current,
                    Attempts = 0,
                    BizNo = bizNo,
                    Source = "idempotent"
                };
            }

            await uow.CompleteAsync();

            var published = await PublishDeductionEventAsync(
                bizNo, product, quantity, remaining, "idempotent");

            return new InventoryDeductResultDto
            {
                Success = true,
                Message = "扣减成功（Redis SETNX + MySQL 唯一索引）",
                RemainingStock = remaining,
                Attempts = 1,
                BizNo = bizNo,
                Source = "idempotent",
                EventPublished = published
            };
        }

        public async Task<InventoryConcurrencyResultDto> RunConcurrencyTestAsync(
            string mode = "atomic",
            int requests = 100,
            int quantity = 1,
            int initialStock = 100,
            int parallelism = 8)
        {
            mode = (mode ?? "atomic").Trim().ToLowerInvariant();
            if (mode != "naive" && mode != "optimistic" && mode != "redis")
            {
                mode = "atomic";
            }

            EnsureQuantity(quantity);
            requests = Math.Clamp(requests, 1, 300);

            // MySQL max_connections 可能很小（本机是 20），限制同时打开的连接数
            parallelism = Math.Clamp(parallelism, 1, 16);

            // 每次压测前重置，保证结果可对比
            await ResetAsync(initialStock);

            var sw = Stopwatch.StartNew();
            using var gate = new SemaphoreSlim(parallelism);

            var tasks = Enumerable.Range(0, requests).Select(async _ =>
            {
                await gate.WaitAsync();
                try
                {
                    // 直接在后台任务中调用当前实例：
                    // 每个扣减方法内部都会 Begin(requiresNew: true)，各自持有独立的 UoW 和 DbContext。
                    // 不新建 DI Scope，避免后台任务拿不到 HttpContext 导致 ABP UoW 拦截器 NRE。
                    var result = mode switch
                    {
                        "naive" => await DeductNaiveAsync(quantity),
                        "optimistic" => await DeductOptimisticAsync(quantity),
                        "redis" => await DeductWithRedisAsync(quantity),
                        _ => await DeductAtomicAsync(quantity)
                    };

                    return result.Success;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "并发压测请求失败，mode={Mode}, quantity={Quantity}", mode, quantity);
                    return false;
                }
                finally
                {
                    gate.Release();
                }
            });

            var outcomes = await Task.WhenAll(tasks);
            sw.Stop();

            var finalStock = await ReadStockInNewUowAsync();
            var successCount = outcomes.Count(x => x);
            var deducted = initialStock - finalStock;

            return new InventoryConcurrencyResultDto
            {
                Mode = mode,
                Requests = requests,
                Parallelism = parallelism,
                Quantity = quantity,
                InitialStock = initialStock,
                FinalStock = finalStock,
                SuccessCount = successCount,
                FailCount = requests - successCount,
                DeductedQuantity = deducted,
                LostUpdates = successCount * quantity - deducted,
                ElapsedMs = sw.ElapsedMilliseconds,
                Note = mode switch
                {
                    "naive" => "错误示范：成功数大于实际扣减，差额就是丢失更新（超卖风险）。",
                    "optimistic" => "乐观锁：库存快照 CAS，冲突时重试。",
                    "redis" => "Redis Lua 预扣 + MySQL 落库，落库失败会回补 Redis。",
                    _ => "MySQL 条件更新：受影响行数为 0 即库存不足。"
                }
            };
        }

        public async Task<PagedResultDto<InventoryAuditLogDto>> GetAuditLogsAsync(int take = 20)
        {
            var connectionString = _configuration.GetConnectionString("mongodb")
                                   ?? "mongodb://localhost:27017/t1_frame";
            var url = new MongoUrl(connectionString);
            var database = _mongoClient.GetDatabase(url.DatabaseName ?? "t1_frame");
            var collection = database.GetCollection<InventoryDeductionLog>("t1_inventory_deduction_log");

            var filter = Builders<InventoryDeductionLog>.Filter.Empty;
            var totalCount = await collection.CountDocumentsAsync(filter);

            var logs = await collection
                .Find(filter)
                .SortByDescending(x => x.created_at)
                .Limit(Math.Clamp(take, 1, 100))
                .ToListAsync();

            var items = logs.Select(x => new InventoryAuditLogDto
            {
                BizNo = x.biz_no,
                ProductId = x.product_id,
                ProductName = x.product_name,
                Quantity = x.quantity,
                RemainingStock = x.remaining_stock,
                Source = x.source,
                CreatedAt = x.created_at
            }).ToList();

            return new PagedResultDto<InventoryAuditLogDto>(totalCount, items);
        }

        public async Task<PagedResultDto<InventoryProductDto>> GetProductsAsync()
        {
            using var uow = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: false);
            var db = await _dbContextProvider.GetDbContextAsync();

            var products = await db.T1GoodsStock
                .AsNoTracking()
                .OrderBy(x => x.Id)
                .ToListAsync();

            var details = await db.T1InventoryProductDetail
                .AsNoTracking()
                .ToDictionaryAsync(x => x.Id);

            await uow.CompleteAsync();

            var items = products
                .Select(product => ToDto(product, details.GetValueOrDefault(product.Id)))
                .ToList();

            return new PagedResultDto<InventoryProductDto>(items.Count, items);
        }

        public async Task<InventoryProductDto> CreateProductAsync(InventoryCreateProductInput input)
        {
            if (string.IsNullOrWhiteSpace(input.ProductName))
            {
                throw new UserFriendlyException("商品名称不能为空");
            }

            if (input.Stock < 0)
            {
                throw new UserFriendlyException("库存不能为负数");
            }

            if (input.Price < 0)
            {
                throw new UserFriendlyException("价格不能为负数");
            }

            using var uow = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: true);
            var db = await _dbContextProvider.GetDbContextAsync();

            var exists = await db.T1GoodsStock
                .AnyAsync(x => x.goods_name == input.ProductName);

            if (exists)
            {
                throw new UserFriendlyException($"商品「{input.ProductName}」已存在");
            }

            var product = new T1GoodsStock
            {
                goods_name = input.ProductName.Trim(),
                goods_stock = input.Stock
            };

            db.T1GoodsStock.Add(product);
            await db.SaveChangesAsync();

            var detail = new T1InventoryProductDetail(product.Id)
            {
                price = input.Price,
                description = input.Description,
                image_url = input.ImageUrl,
                category = input.Category,
                is_on_sale = input.IsOnSale,
                created_at = DateTime.Now
            };

            db.T1InventoryProductDetail.Add(detail);
            await db.SaveChangesAsync();
            await uow.CompleteAsync();

            await TrySetRedisStockAsync(product.Id, product.goods_stock);

            return ToDto(product, detail);
        }

        public async Task<InventoryProductDto> SetStockAsync(long productId, int stock)
        {
            if (stock < 0)
            {
                throw new UserFriendlyException("库存不能为负数");
            }

            using var uow = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: true);
            var db = await _dbContextProvider.GetDbContextAsync();

            var product = await db.T1GoodsStock
                .FirstOrDefaultAsync(x => x.Id == productId);

            if (product == null)
            {
                throw new UserFriendlyException("商品不存在");
            }

            product.goods_stock = stock;
            await db.SaveChangesAsync();
            await uow.CompleteAsync();

            // 管理端调整库存后同步 Redis，避免预扣层继续使用旧值
            await TrySetRedisStockAsync(product.Id, stock);

            var detail = await db.T1InventoryProductDetail
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == product.Id);

            return ToDto(product, detail);
        }

        public async Task<InventoryProductDto> UpdateProductAsync(
            long productId,
            InventoryUpdateProductInput input)
        {
            if (input.Price < 0)
            {
                throw new UserFriendlyException("价格不能为负数");
            }

            using var uow = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: true);
            var db = await _dbContextProvider.GetDbContextAsync();

            var product = await db.T1GoodsStock
                .FirstOrDefaultAsync(x => x.Id == productId);

            if (product == null)
            {
                throw new UserFriendlyException("商品不存在");
            }

            var detail = await db.T1InventoryProductDetail
                .FirstOrDefaultAsync(x => x.Id == productId);

            if (detail == null)
            {
                detail = new T1InventoryProductDetail(productId);
                db.T1InventoryProductDetail.Add(detail);
            }

            detail.price = input.Price;
            detail.description = input.Description;
            detail.image_url = input.ImageUrl;
            detail.category = input.Category;
            detail.is_on_sale = input.IsOnSale;

            await db.SaveChangesAsync();
            await uow.CompleteAsync();

            return ToDto(product, detail);
        }

        public async Task<PagedResultDto<InventoryOrderDto>> GetOrdersAsync(
            int skip = 0,
            int take = 20,
            int? status = null)
        {
            skip = Math.Max(0, skip);
            take = Math.Clamp(take, 1, 200);

            using var uow = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: false);
            var db = await _dbContextProvider.GetDbContextAsync();

            var query = db.T1InventoryOrder.AsNoTracking();

            if (status.HasValue)
            {
                query = query.Where(x => x.status == status.Value);
            }

            var totalCount = await query.LongCountAsync();

            var orders = await query
                .OrderByDescending(x => x.Id)
                .Skip(skip)
                .Take(take)
                .ToListAsync();

            var productIds = orders.Select(x => x.product_id).Distinct().ToList();
            var productNames = await db.T1GoodsStock
                .AsNoTracking()
                .Where(x => productIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.goods_name);

            var items = orders.Select(x => new InventoryOrderDto
            {
                Id = x.Id,
                OrderNo = x.order_no,
                SaleNo = GetSaleKey(x),
                ProductId = x.product_id,
                ProductName = productNames.GetValueOrDefault(x.product_id, "-"),
                Quantity = x.quantity,
                UnitPrice = x.unit_price,
                TotalAmount = x.unit_price * x.quantity,
                CustomerName = x.customer_name,
                Status = x.status,
                CreatedAt = x.created_at
            }).ToList();

            await uow.CompleteAsync();

            return new PagedResultDto<InventoryOrderDto>(totalCount, items);
        }

        /// <summary>
        /// 商店下单：一次结算多个商品，逐个条件扣减库存，整单在同一事务中提交。
        /// </summary>
        public async Task<InventorySaleResultDto> CreateSaleAsync(InventoryCreateSaleInput input)
        {
            if (input.Items == null || input.Items.Count == 0)
            {
                throw new UserFriendlyException("请选择商品");
            }

            var items = input.Items
                .GroupBy(x => x.ProductId)
                .Select(group => new InventorySaleItemInput
                {
                    ProductId = group.Key,
                    Quantity = group.Sum(x => x.Quantity)
                })
                .ToList();

            if (items.Any(x => x.Quantity <= 0))
            {
                throw new UserFriendlyException("购买数量必须大于 0");
            }

            var saleNo = NormalizeSaleNo(input.RequestId);
            var customerName = string.IsNullOrWhiteSpace(input.CustomerName)
                ? null
                : input.CustomerName.Trim();

            if (customerName?.Length > 100)
            {
                customerName = customerName[..100];
            }

            using var uow = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: true);
            var db = await _dbContextProvider.GetDbContextAsync();

            var productIds = items.Select(x => x.ProductId).Distinct().ToList();

            var products = await db.T1GoodsStock
                .Where(x => productIds.Contains(x.Id))
                .ToListAsync();

            var productMap = products.ToDictionary(x => x.Id);

            var details = await db.T1InventoryProductDetail
                .Where(x => productIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id);

            // 幂等：同一请求号已经生成过订单，直接返回原结果
            var orderPrefix = $"{saleNo}-";
            var existingOrders = await db.T1InventoryOrder
                .AsNoTracking()
                .Where(x => x.sale_no == saleNo || x.order_no.StartsWith(orderPrefix))
                .OrderBy(x => x.order_no)
                .ToListAsync();

            if (existingOrders.Count > 0)
            {
                var existingItems = existingOrders.Select(order =>
                {
                    var product = productMap.GetValueOrDefault(order.product_id);
                    return new InventorySaleItemResultDto
                    {
                        ProductId = order.product_id,
                        OrderNo = order.order_no,
                        ProductName = product?.goods_name ?? "-",
                        Quantity = order.quantity,
                        UnitPrice = order.unit_price,
                        TotalAmount = order.unit_price * order.quantity,
                        RemainingStock = product?.goods_stock ?? 0
                    };
                }).ToList();

                await uow.CompleteAsync();

                return new InventorySaleResultDto
                {
                    Success = true,
                    SaleNo = saleNo,
                    Message = "重复请求，已返回原订单",
                    TotalAmount = existingItems.Sum(x => x.TotalAmount),
                    Items = existingItems
                };
            }

            // 先整体校验，避免部分商品扣减后才发现另一个商品不可售
            foreach (var item in items)
            {
                if (!productMap.TryGetValue(item.ProductId, out var product))
                {
                    await uow.RollbackAsync();
                    return FailSale(saleNo, $"商品 {item.ProductId} 不存在");
                }

                if (!details.TryGetValue(item.ProductId, out var detail) || !detail.is_on_sale)
                {
                    await uow.RollbackAsync();
                    return FailSale(saleNo, $"商品「{product.goods_name}」已下架");
                }

                if (product.goods_stock < item.Quantity)
                {
                    await uow.RollbackAsync();
                    return FailSale(saleNo, $"商品「{product.goods_name}」库存不足");
                }
            }

            var results = new List<InventorySaleItemResultDto>();
            var now = DateTime.Now;

            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var product = productMap[item.ProductId];
                var detail = details[item.ProductId];

                var affected = await db.T1GoodsStock
                    .Where(x => x.Id == item.ProductId && x.goods_stock >= item.Quantity)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(x => x.goods_stock, x => x.goods_stock - item.Quantity));

                if (affected == 0)
                {
                    await uow.RollbackAsync();
                    return FailSale(saleNo, $"商品「{product.goods_name}」库存不足");
                }

                var remaining = await ReadStockAsync(db, item.ProductId);
                var orderNo = $"{saleNo}-{i + 1:D2}";

                await db.T1InventoryOrder.AddAsync(new T1InventoryOrder
                {
                    order_no = orderNo,
                    sale_no = saleNo,
                    product_id = item.ProductId,
                    quantity = item.Quantity,
                    unit_price = detail.price,
                    customer_name = customerName,
                    status = 1,
                    created_at = now
                });

                await db.T1InventoryStockFlow.AddAsync(new T1InventoryStockFlow
                {
                    biz_no = orderNo,
                    product_id = item.ProductId,
                    product_name = product.goods_name,
                    quantity = item.Quantity,
                    remaining_stock = remaining,
                    source = "sale",
                    created_at = now
                });

                results.Add(new InventorySaleItemResultDto
                {
                    ProductId = item.ProductId,
                    OrderNo = orderNo,
                    ProductName = product.goods_name,
                    Quantity = item.Quantity,
                    UnitPrice = detail.price,
                    TotalAmount = detail.price * item.Quantity,
                    RemainingStock = remaining
                });
            }

            await db.SaveChangesAsync();
            await uow.CompleteAsync();

            foreach (var result in results)
            {
                await PublishDeductionEventAsync(
                    result.OrderNo,
                    productMap[result.ProductId],
                    result.Quantity,
                    result.RemainingStock,
                    "sale");
            }

            return new InventorySaleResultDto
            {
                Success = true,
                SaleNo = saleNo,
                Message = "下单成功",
                TotalAmount = results.Sum(x => x.TotalAmount),
                Items = results
            };
        }

        public async Task<InventoryOrderDto> PayOrderAsync(long orderId)
        {
            using var uow = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: true);
            var db = await _dbContextProvider.GetDbContextAsync();

            var order = await db.T1InventoryOrder
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == orderId);

            if (order == null)
            {
                throw new UserFriendlyException("订单不存在");
            }

            if (order.status == 5)
            {
                throw new UserFriendlyException("已关闭的订单不能支付");
            }

            if (order.status == 1)
            {
                var affected = await db.T1InventoryOrder
                    .Where(x => x.Id == orderId && x.status == 1)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(x => x.status, 2));

                if (affected == 0)
                {
                    throw new UserFriendlyException("订单状态已变化，请刷新后重试");
                }
            }

            var updated = await db.T1InventoryOrder
                .AsNoTracking()
                .FirstAsync(x => x.Id == orderId);

            var productName = await db.T1GoodsStock
                .AsNoTracking()
                .Where(x => x.Id == updated.product_id)
                .Select(x => x.goods_name)
                .FirstOrDefaultAsync() ?? "-";

            await uow.CompleteAsync();

            return ToOrderDto(updated, productName);
        }

        public async Task<PagedResultDto<InventorySaleGroupDto>> GetSaleOrdersAsync(
            int skip = 0,
            int take = 20,
            int? status = null)
        {
            skip = Math.Max(0, skip);
            take = Math.Clamp(take, 1, 100);

            using var uow = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: false);
            var db = await _dbContextProvider.GetDbContextAsync();

            var orders = await db.T1InventoryOrder
                .AsNoTracking()
                .OrderByDescending(x => x.Id)
                .Take(2000)
                .ToListAsync();

            var productIds = orders.Select(x => x.product_id).Distinct().ToList();
            var productNames = await db.T1GoodsStock
                .AsNoTracking()
                .Where(x => productIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.goods_name);

            var groups = orders
                .GroupBy(GetSaleKey)
                .Select(group => CreateSaleGroup(
                    group.Key,
                    group.OrderBy(x => x.order_no).ToList(),
                    productNames))
                .ToList();

            if (status.HasValue)
            {
                groups = groups.Where(x => x.Status == status.Value).ToList();
            }

            var totalCount = groups.Count;
            var items = groups
                .OrderByDescending(x => x.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToList();

            await uow.CompleteAsync();

            return new PagedResultDto<InventorySaleGroupDto>(totalCount, items);
        }

        public async Task<InventorySaleGroupDto> PaySaleAsync(string saleNo)
        {
            if (string.IsNullOrWhiteSpace(saleNo))
            {
                throw new UserFriendlyException("销售单号不能为空");
            }

            var prefix = $"{saleNo}-";

            using var uow = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: true);
            var db = await _dbContextProvider.GetDbContextAsync();

            var orders = await db.T1InventoryOrder
                .Where(x => x.sale_no == saleNo || x.order_no.StartsWith(prefix) || x.order_no == saleNo)
                .ToListAsync();

            if (orders.Count == 0)
            {
                throw new UserFriendlyException("销售单不存在");
            }

            if (orders.Any(x => x.status == 5))
            {
                throw new UserFriendlyException("已取消的销售单不能支付");
            }

            if (orders.Any(x => x.status != 1 && x.status != 2))
            {
                throw new UserFriendlyException("订单状态不允许支付");
            }

            if (orders.Any(x => x.status == 1))
            {
                var affected = await db.T1InventoryOrder
                    .Where(x => (x.sale_no == saleNo || x.order_no.StartsWith(prefix) || x.order_no == saleNo)
                                && x.status == 1)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(x => x.status, 2));

                if (affected == 0)
                {
                    throw new UserFriendlyException("订单状态已变化，请刷新后重试");
                }
            }

            var updated = await db.T1InventoryOrder
                .AsNoTracking()
                .Where(x => x.sale_no == saleNo || x.order_no.StartsWith(prefix) || x.order_no == saleNo)
                .OrderBy(x => x.order_no)
                .ToListAsync();

            var group = await BuildSaleGroupAsync(db, saleNo, updated);

            await uow.CompleteAsync();

            return group;
        }

        public async Task<InventorySaleGroupDto> CancelSaleAsync(string saleNo)
        {
            if (string.IsNullOrWhiteSpace(saleNo))
            {
                throw new UserFriendlyException("销售单号不能为空");
            }

            var prefix = $"{saleNo}-";

            using var uow = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: true);
            var db = await _dbContextProvider.GetDbContextAsync();

            var orders = await db.T1InventoryOrder
                .Where(x => x.sale_no == saleNo || x.order_no.StartsWith(prefix) || x.order_no == saleNo)
                .OrderBy(x => x.order_no)
                .ToListAsync();

            if (orders.Count == 0)
            {
                throw new UserFriendlyException("销售单不存在");
            }

            var productIds = orders.Select(x => x.product_id).Distinct().ToList();
            var productNames = await db.T1GoodsStock
                .AsNoTracking()
                .Where(x => productIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.goods_name);

            foreach (var order in orders.Where(x => x.status != 5))
            {
                if (order.status != 1 && order.status != 2)
                {
                    throw new UserFriendlyException("订单状态不允许取消");
                }

                var cancelled = await db.T1InventoryOrder
                    .Where(x => x.Id == order.Id && (x.status == 1 || x.status == 2))
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(x => x.status, 5));

                if (cancelled == 0)
                {
                    var currentStatus = await db.T1InventoryOrder
                        .AsNoTracking()
                        .Where(x => x.Id == order.Id)
                        .Select(x => x.status)
                        .FirstAsync();

                    if (currentStatus != 5)
                    {
                        throw new UserFriendlyException("订单状态已变化，请刷新后重试");
                    }

                    continue;
                }

                var restored = await db.T1GoodsStock
                    .Where(x => x.Id == order.product_id)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(x => x.goods_stock, x => x.goods_stock + order.quantity));

                if (restored == 0)
                {
                    throw new UserFriendlyException("商品已删除，无法回补库存");
                }

                var remaining = await ReadStockAsync(db, order.product_id);

                await db.T1InventoryStockFlow.AddAsync(new T1InventoryStockFlow
                {
                    biz_no = $"CANCEL-{order.order_no}",
                    product_id = order.product_id,
                    product_name = productNames.GetValueOrDefault(order.product_id, "-"),
                    quantity = order.quantity,
                    remaining_stock = remaining,
                    source = "cancel",
                    created_at = DateTime.Now
                });
            }

            await db.SaveChangesAsync();

            var updated = await db.T1InventoryOrder
                .AsNoTracking()
                .Where(x => x.sale_no == saleNo || x.order_no.StartsWith(prefix) || x.order_no == saleNo)
                .OrderBy(x => x.order_no)
                .ToListAsync();

            var group = CreateSaleGroup(saleNo, updated, productNames);

            await uow.CompleteAsync();

            return group;
        }

        public async Task<InventorySaleGroupDto> CancelOrderAsync(long orderId)
        {
            using (var uow = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: false))
            {
                var db = await _dbContextProvider.GetDbContextAsync();

                var order = await db.T1InventoryOrder
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == orderId);

                if (order == null)
                {
                    throw new UserFriendlyException("订单不存在");
                }

                var saleNo = GetSaleKey(order);
                await uow.CompleteAsync();

                // 属于销售单时取消整张销售单；库存演示订单则只取消这一行
                return await CancelSaleAsync(saleNo);
            }
        }

        public async Task<PagedResultDto<InventoryStockFlowDto>> GetStockFlowsAsync(
            int skip = 0,
            int take = 20)
        {
            skip = Math.Max(0, skip);
            take = Math.Clamp(take, 1, 200);

            using var uow = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: false);
            var db = await _dbContextProvider.GetDbContextAsync();

            var query = db.T1InventoryStockFlow.AsNoTracking();
            var totalCount = await query.LongCountAsync();

            var flows = await query
                .OrderByDescending(x => x.Id)
                .Skip(skip)
                .Take(take)
                .ToListAsync();

            var items = flows.Select(x => new InventoryStockFlowDto
            {
                Id = x.Id,
                BizNo = x.biz_no,
                ProductId = x.product_id,
                ProductName = x.product_name,
                Quantity = x.quantity,
                RemainingStock = x.remaining_stock,
                Source = x.source,
                CreatedAt = x.created_at
            }).ToList();

            await uow.CompleteAsync();

            return new PagedResultDto<InventoryStockFlowDto>(totalCount, items);
        }

        public async Task<InventoryDashboardDto> GetDashboardAsync()
        {
            using var uow = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: false);
            var db = await _dbContextProvider.GetDbContextAsync();

            var productCount = await db.T1GoodsStock.CountAsync();
            var totalStock = await db.T1GoodsStock.SumAsync(x => (int?)x.goods_stock) ?? 0;
            var lowStockCount = await db.T1GoodsStock.CountAsync(x => x.goods_stock <= 10);
            var orderCount = await db.T1InventoryOrder.CountAsync();

            await uow.CompleteAsync();

            return new InventoryDashboardDto
            {
                ProductCount = productCount,
                TotalStock = totalStock,
                LowStockCount = lowStockCount,
                OrderCount = orderCount
            };
        }

        private async Task<T1GoodsStock> GetOrCreateProductAsync(T1FrameAbpDbContext db)
        {
            var product = await db.T1GoodsStock
                .FirstOrDefaultAsync(x => x.goods_name == DemoProductName);

            if (product != null)
            {
                return product;
            }

            product = new T1GoodsStock
            {
                goods_name = DemoProductName,
                goods_stock = 100
            };

            db.T1GoodsStock.Add(product);
            await db.SaveChangesAsync();

            return product;
        }

        private static async Task<int> ReadStockAsync(T1FrameAbpDbContext db, long productId)
        {
            return await db.T1GoodsStock
                .AsNoTracking()
                .Where(x => x.Id == productId)
                .Select(x => x.goods_stock)
                .FirstAsync();
        }

        private async Task<int> ReadStockInNewUowAsync()
        {
            using var uow = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: false);
            var db = await _dbContextProvider.GetDbContextAsync();

            var product = await db.T1GoodsStock
                .AsNoTracking()
                .FirstAsync(x => x.goods_name == DemoProductName);

            var stock = product.goods_stock;
            await uow.CompleteAsync();
            return stock;
        }

        private async Task<bool> PublishDeductionEventAsync(
            string bizNo,
            T1GoodsStock product,
            int quantity,
            int remainingStock,
            string source)
        {
            try
            {
                await _distributedEventBus.PublishAsync(new StockDeductedEto
                {
                    biz_no = bizNo,
                    product_id = product.Id,
                    product_name = product.goods_name,
                    quantity = quantity,
                    remaining_stock = remainingStock,
                    source = source,
                    occurred_at = DateTime.Now
                });

                return true;
            }
            catch (Exception ex)
            {
                // MQ 故障不应影响已经提交的扣减结果
                _logger.LogWarning(ex, "库存扣减事件发布失败，bizNo={BizNo}", bizNo);
                return false;
            }
        }

        private async Task TrySetRedisStockAsync(long productId, int stock)
        {
            try
            {
                await _redis.GetDatabase().StringSetAsync(StockKey(productId), stock);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis 库存预热失败，productId={ProductId}", productId);
            }
        }

        private async Task TryDeleteIdempotentKeyAsync(string key)
        {
            try
            {
                await _redis.GetDatabase().KeyDeleteAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis 幂等键删除失败，key={Key}", key);
            }
        }

        private static string StockKey(long productId) => $"inventory:stock:{productId}";

        private static string IdempotentKey(string bizNo) => $"inventory:idem:{bizNo}";

        private static T1InventoryStockFlow NewFlow(
            T1GoodsStock product,
            string bizNo,
            int quantity,
            int remainingStock,
            string source)
        {
            return new T1InventoryStockFlow
            {
                biz_no = bizNo,
                product_id = product.Id,
                product_name = product.goods_name,
                quantity = quantity,
                remaining_stock = remainingStock,
                source = source,
                created_at = DateTime.Now
            };
        }

        private static T1InventoryOrder NewOrder(string bizNo, long productId, int quantity)
        {
            return new T1InventoryOrder
            {
                order_no = $"ORD-{bizNo}",
                product_id = productId,
                quantity = quantity,
                status = 1,
                created_at = DateTime.Now
            };
        }

        private static InventoryProductDto ToDto(
            T1GoodsStock product,
            T1InventoryProductDetail? detail)
        {
            return new InventoryProductDto
            {
                Id = product.Id,
                ProductName = product.goods_name,
                Stock = product.goods_stock,
                Price = detail?.price ?? 0,
                Description = detail?.description,
                ImageUrl = detail?.image_url,
                Category = detail?.category,
                IsOnSale = detail?.is_on_sale ?? false
            };
        }

        private static string GetSaleKey(T1InventoryOrder order)
        {
            if (!string.IsNullOrWhiteSpace(order.sale_no))
            {
                return order.sale_no;
            }

            // 兼容早期没有 sale_no 的商店订单：SALE...-01 -> SALE...
            if (order.order_no.StartsWith("SALE", StringComparison.OrdinalIgnoreCase))
            {
                var index = order.order_no.LastIndexOf('-');
                if (index > 0)
                {
                    return order.order_no[..index];
                }
            }

            return order.order_no;
        }

        private static InventorySaleGroupDto CreateSaleGroup(
            string saleNo,
            List<T1InventoryOrder> orders,
            Dictionary<long, string> productNames)
        {
            var status = orders.All(x => x.status == 5)
                ? 5
                : orders.Any(x => x.status == 1)
                    ? 1
                    : orders.Any(x => x.status == 2)
                        ? 2
                        : 5;

            return new InventorySaleGroupDto
            {
                SaleNo = saleNo,
                CustomerName = orders
                    .Select(x => x.customer_name)
                    .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)),
                Status = status,
                TotalAmount = orders.Sum(x => x.unit_price * x.quantity),
                TotalQuantity = orders.Sum(x => x.quantity),
                OrderCount = orders.Count,
                CreatedAt = orders.Min(x => x.created_at),
                Items = orders
                    .Select(x => ToOrderDto(x, productNames.GetValueOrDefault(x.product_id, "-")))
                    .ToList()
            };
        }

        private static async Task<InventorySaleGroupDto> BuildSaleGroupAsync(
            T1FrameAbpDbContext db,
            string saleNo,
            List<T1InventoryOrder> orders)
        {
            var productIds = orders.Select(x => x.product_id).Distinct().ToList();

            var productNames = await db.T1GoodsStock
                .AsNoTracking()
                .Where(x => productIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.goods_name);

            return CreateSaleGroup(saleNo, orders, productNames);
        }

        private static InventoryOrderDto ToOrderDto(T1InventoryOrder order, string productName)
        {
            return new InventoryOrderDto
            {
                Id = order.Id,
                OrderNo = order.order_no,
                SaleNo = GetSaleKey(order),
                ProductId = order.product_id,
                ProductName = productName,
                Quantity = order.quantity,
                UnitPrice = order.unit_price,
                TotalAmount = order.unit_price * order.quantity,
                CustomerName = order.customer_name,
                Status = order.status,
                CreatedAt = order.created_at
            };
        }

        private static string NormalizeSaleNo(string? requestId)
        {
            var value = new string((requestId ?? string.Empty)
                .Where(char.IsLetterOrDigit)
                .ToArray());

            if (string.IsNullOrWhiteSpace(value))
            {
                value = Guid.NewGuid().ToString("N");
            }

            return value.Length > 30 ? value[..30] : value;
        }

        private static InventorySaleResultDto FailSale(string saleNo, string message)
        {
            return new InventorySaleResultDto
            {
                Success = false,
                SaleNo = saleNo,
                Message = message
            };
        }

        private static void EnsureQuantity(int quantity)
        {
            if (quantity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(quantity), "扣减数量必须大于 0");
            }
        }

        private static InventoryDeductResultDto Fail(
            string message,
            int remainingStock,
            string source = "")
        {
            return new InventoryDeductResultDto
            {
                Success = false,
                Message = message,
                RemainingStock = remainingStock,
                Attempts = 1,
                Source = source
            };
        }
    }
}
