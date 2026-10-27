using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using t1_frame.entityframeworkcore.abp;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Uow;

namespace t1_frame.application.abp.Seckill
{
    /// <summary>
    /// 1000 QPS 秒杀入口：
    /// Redis 分桶 Lua 预扣 + 同一个脚本内写入 Redis List outbox。
    /// 请求路径只访问 Redis；outbox relay 负责投递 Kafka，消费者批量落库。
    /// 说明：使用 List 而不是 Stream，是为了兼容现场仅支持基础命令的 Redis 实例。
    /// </summary>
    public sealed class SeckillService
    {
        public const int BucketCount = 8;

        internal const string OutboxKey = "seckill:outbox";

        internal const string ProcessingKey = "seckill:outbox:processing";

        private const long MaxOutboxLength = 100_000;

        private const string DeductLua = @"
if redis.call('EXISTS', KEYS[1]) == 1 then
    return -3
end

local backlog = redis.call('LLEN', KEYS[2]) + redis.call('LLEN', KEYS[3])
if backlog >= tonumber(ARGV[6]) then
    return -4
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

        -- requestId 已经过滤为字母/数字/-/_，不会破坏 JSON 结构。
        local payload = '{""RunId"":' .. ARGV[3]
            .. ',""ProductId"":' .. ARGV[5]
            .. ',""RequestId"":""' .. ARGV[4] .. '""'
            .. ',""Quantity"":' .. ARGV[1]
            .. ',""Bucket"":' .. slot .. '}'

        -- LPUSH + RPOPLPUSH 组成可靠的 FIFO outbox
        redis.call('LPUSH', KEYS[2], payload)
        return slot + 1
    end
end

return 0";

        private readonly IConnectionMultiplexer _redis;
        private readonly SeckillQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SeckillService> _logger;

        private long _runId = DateTime.UtcNow.Ticks;

        public SeckillService(
            IConnectionMultiplexer redis,
            SeckillQueue queue,
            IServiceScopeFactory scopeFactory,
            ILogger<SeckillService> logger)
        {
            _redis = redis;
            _queue = queue;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task<SeckillResultDto> SeckillAsync(
            long productId,
            int quantity,
            string? requestId)
        {
            if (quantity <= 0 || quantity > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(quantity), "秒杀数量必须在 1 到 100 之间");
            }

            requestId = NormalizeRequestId(requestId);

            var runId = Interlocked.Read(ref _runId);
            var prefix = Prefix(runId);
            var redis = _redis.GetDatabase();

            var idemKey = $"{prefix}:idem:{requestId}";
            var bucketKeys = Enumerable.Range(0, BucketCount)
                .Select(index => (RedisKey)BucketKey(runId, productId, index))
                .ToArray();

            // KEYS[1] 幂等键，KEYS[2] outbox，KEYS[3] processing，KEYS[4..] 分桶库存
            var keys = new RedisKey[bucketKeys.Length + 3];
            keys[0] = idemKey;
            keys[1] = OutboxKey;
            keys[2] = ProcessingKey;
            Array.Copy(bucketKeys, 0, keys, 3, bucketKeys.Length);

            var startBucket = GetStartBucket(requestId);

            long result;
            try
            {
                result = (long)await redis.ScriptEvaluateAsync(
                    DeductLua,
                    keys,
                    new RedisValue[]
                    {
                        quantity,
                        startBucket,
                        runId,
                        requestId,
                        productId,
                        MaxOutboxLength
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "秒杀 Redis Lua 执行失败：productId={ProductId}, requestId={RequestId}",
                    productId,
                    requestId);

                return new SeckillResultDto
                {
                    Accepted = false,
                    RequestId = requestId,
                    Status = "error",
                    Message = "redis error"
                };
            }

            var statusKey = StatusKey(runId, requestId);

            if (result == -3)
            {
                var existing = await redis.StringGetAsync(statusKey);
                return new SeckillResultDto
                {
                    Accepted = true,
                    RequestId = requestId,
                    Status = existing.IsNull ? "accepted" : existing.ToString(),
                    Message = "duplicate request"
                };
            }

            if (result == -4)
            {
                return new SeckillResultDto
                {
                    Accepted = false,
                    RequestId = requestId,
                    Status = "busy",
                    Message = "seckill outbox backlog is too large"
                };
            }

            if (result == 0)
            {
                return new SeckillResultDto
                {
                    Accepted = false,
                    RequestId = requestId,
                    Status = "soldout",
                    Message = "sold out"
                };
            }

            var bucket = (int)result - 1;
            if (bucket < 0 || bucket >= BucketCount)
            {
                return new SeckillResultDto
                {
                    Accepted = false,
                    RequestId = requestId,
                    Status = "error",
                    Message = "invalid bucket"
                };
            }

            // Lua 已把消息写入 outbox，这里只记录查询状态和进程内指标。
            await redis.StringSetAsync(statusKey, "accepted", TimeSpan.FromHours(1));
            _queue.MarkAccepted();

            return new SeckillResultDto
            {
                Accepted = true,
                RequestId = requestId,
                Status = "accepted",
                Bucket = bucket + 1,
                Message = "queued"
            };
        }

        public async Task<SeckillStatusDto> GetStatusAsync(string requestId)
        {
            var runId = Interlocked.Read(ref _runId);
            var value = await _redis.GetDatabase().StringGetAsync(StatusKey(runId, requestId));

            return new SeckillStatusDto
            {
                RequestId = requestId,
                Status = value.IsNull ? "unknown" : value.ToString()
            };
        }

        public async Task<SeckillStatsDto> GetStatsAsync(long productId)
        {
            var runId = Interlocked.Read(ref _runId);
            var prefix = Prefix(runId);
            var redis = _redis.GetDatabase();

            var keys = Enumerable.Range(0, BucketCount)
                .Select(index => (RedisKey)BucketKey(runId, productId, index))
                .ToArray();

            var values = await redis.StringGetAsync(keys);
            var outboxLength = await redis.ListLengthAsync(OutboxKey)
                + await redis.ListLengthAsync(ProcessingKey);
            var buckets = new List<SeckillBucketDto>(BucketCount);
            var total = 0;

            for (var i = 0; i < values.Length; i++)
            {
                var stock = values[i].IsNull ? 0 : (int)values[i];
                total += stock;
                buckets.Add(new SeckillBucketDto { Bucket = i + 1, Stock = stock });
            }

            return new SeckillStatsDto
            {
                RunId = runId,
                ProductId = productId,
                TotalStock = total,
                Accepted = _queue.Accepted,
                Created = _queue.Created,
                Failed = _queue.Failed,
                Pending = _queue.Pending,
                OutboxLength = outboxLength,
                Buckets = buckets
            };
        }

        public async Task<SeckillResetDto> ResetAsync(long productId, int stock)
        {
            if (stock < BucketCount)
            {
                throw new ArgumentOutOfRangeException(nameof(stock), "库存不能小于分桶数");
            }

            await WaitForIdleAsync(TimeSpan.FromSeconds(20));

            await ResetDatabaseAsync(productId, stock);

            var runId = DateTime.UtcNow.Ticks;
            Interlocked.Exchange(ref _runId, runId);

            var prefix = Prefix(runId);
            var redis = _redis.GetDatabase();
            var perBucket = stock / BucketCount;
            var remainder = stock % BucketCount;

            for (var i = 0; i < BucketCount; i++)
            {
                var value = perBucket + (i < remainder ? 1 : 0);
                await redis.StringSetAsync(
                    BucketKey(runId, productId, i),
                    value,
                    TimeSpan.FromHours(2));
            }

            _queue.ResetCounters();

            _logger.LogInformation(
                "Seckill reset: productId={ProductId}, stock={Stock}, buckets={Buckets}, runId={RunId}",
                productId,
                stock,
                BucketCount,
                runId);

            return new SeckillResetDto
            {
                RunId = runId,
                ProductId = productId,
                TotalStock = stock,
                BucketCount = BucketCount
            };
        }

        private async Task WaitForIdleAsync(TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow.Add(timeout);
            var redis = _redis.GetDatabase();

            while (DateTime.UtcNow < deadline)
            {
                var outboxLength = await redis.ListLengthAsync(OutboxKey)
                    + await redis.ListLengthAsync(ProcessingKey);
                if (_queue.Pending <= 0 && outboxLength == 0)
                {
                    return;
                }

                await Task.Delay(100);
            }

            var remaining = await redis.ListLengthAsync(OutboxKey)
                + await redis.ListLengthAsync(ProcessingKey);
            throw new InvalidOperationException(
                $"秒杀链路尚未清空（pending={_queue.Pending}, outbox={remaining}），请确认 Kafka 消费者正常后再重置");
        }

        private async Task ResetDatabaseAsync(long productId, int stock)
        {
            using var scope = _scopeFactory.CreateScope();

            var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
            using var uow = uowManager.Begin(requiresNew: true, isTransactional: true);

            var provider = scope.ServiceProvider
                .GetRequiredService<IDbContextProvider<T1FrameAbpDbContext>>();

            var db = await provider.GetDbContextAsync();

            // 演示环境：清理之前所有秒杀订单/流水
            await db.T1InventoryOrder
                .Where(x => x.order_no.StartsWith("SK-"))
                .ExecuteDeleteAsync();

            await db.T1InventoryStockFlow
                .Where(x => x.biz_no.StartsWith("SK-"))
                .ExecuteDeleteAsync();

            var affected = await db.T1GoodsStock
                .Where(x => x.Id == productId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.goods_stock, stock));

            if (affected == 0)
            {
                await uow.RollbackAsync();
                throw new InvalidOperationException("商品不存在，无法重置秒杀库存");
            }

            await uow.CompleteAsync();
        }

        private static int GetStartBucket(string requestId)
        {
            unchecked
            {
                var hash = 2166136261u;

                foreach (var character in requestId)
                {
                    hash ^= character;
                    hash *= 16777619u;
                }

                return (int)(hash % BucketCount);
            }
        }

        private static string NormalizeRequestId(string? requestId)
        {
            var value = new string((requestId ?? string.Empty)
                .Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_')
                .ToArray());

            if (string.IsNullOrWhiteSpace(value))
            {
                value = Guid.NewGuid().ToString("N");
            }

            return value.Length > 40 ? value[..40] : value;
        }

        internal static string OrderNo(long runId, string requestId)
        {
            // runId 是 18 位 Ticks，避免只取后 6 位时不同活动在 100 秒内发生碰撞。
            // order_no 为 varchar(64)，最长为 SK- + 18 + - + 40 = 62。
            return $"SK-{runId}-{requestId}";
        }

        internal static string Prefix(long runId) => $"seckill:{runId}";

        internal static string StatusKey(long runId, string requestId)
        {
            return $"{Prefix(runId)}:status:{requestId}";
        }

        internal static string BucketKey(long runId, long productId, int bucket)
        {
            return $"{Prefix(runId)}:stock:{productId}:{bucket}";
        }
    }
}
