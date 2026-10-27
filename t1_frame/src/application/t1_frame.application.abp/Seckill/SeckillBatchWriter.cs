using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using t1_frame.entityframeworkcore.abp;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Uow;

namespace t1_frame.application.abp.Seckill
{
    /// <summary>
    /// 秒杀订单批量落库。Kafka 至少一次投递，因此这里必须幂等：
    /// 已存在的 order_no 不再扣减库存，也不会重复插入订单/流水。
    /// </summary>
    public sealed class SeckillBatchWriter
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConnectionMultiplexer _redis;

        public SeckillBatchWriter(
            IServiceScopeFactory scopeFactory,
            IConnectionMultiplexer redis)
        {
            _scopeFactory = scopeFactory;
            _redis = redis;
        }

        /// <summary>
        /// 写入一个批次，返回本批次真正新增的订单数（重复消息返回 0）。
        /// </summary>
        public async Task<int> WriteAsync(
            List<SeckillOrderMessage> batch,
            CancellationToken cancellationToken)
        {
            if (batch.Count == 0)
            {
                return 0;
            }

            // 同一批次内的重复消息先按 order_no 去重
            var messages = batch
                .GroupBy(x => SeckillService.OrderNo(x.RunId, x.RequestId))
                .Select(group => group.First())
                .ToList();

            using var scope = _scopeFactory.CreateScope();

            var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
            using var uow = uowManager.Begin(requiresNew: true, isTransactional: true);

            var provider = scope.ServiceProvider
                .GetRequiredService<IDbContextProvider<T1FrameAbpDbContext>>();

            var db = await provider.GetDbContextAsync();
            var now = DateTime.Now;

            var orderNos = messages
                .Select(x => SeckillService.OrderNo(x.RunId, x.RequestId))
                .ToList();

            var existingOrderNos = await db.T1InventoryOrder
                .AsNoTracking()
                .Where(x => orderNos.Contains(x.order_no))
                .Select(x => x.order_no)
                .ToListAsync(cancellationToken);

            var existingSet = existingOrderNos.ToHashSet(StringComparer.Ordinal);
            var newMessages = messages
                .Where(x => !existingSet.Contains(SeckillService.OrderNo(x.RunId, x.RequestId)))
                .ToList();

            if (newMessages.Count == 0)
            {
                await uow.CompleteAsync();
                return 0;
            }

            var bucketRemaining = await ReadBucketRemainingAsync(newMessages);

            foreach (var group in newMessages.GroupBy(x => x.ProductId))
            {
                var product = await db.T1GoodsStock
                    .FirstOrDefaultAsync(x => x.Id == group.Key, cancellationToken);

                if (product == null)
                {
                    throw new InvalidOperationException($"商品 {group.Key} 不存在");
                }

                var detail = await db.T1InventoryProductDetail
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == group.Key, cancellationToken);

                var price = detail?.price ?? 0;
                var totalQuantity = group.Sum(x => x.Quantity);

                // 一个批次只更新一次库存行，避免 1000 次行锁竞争
                var affected = await db.T1GoodsStock
                    .Where(x => x.Id == group.Key && x.goods_stock >= totalQuantity)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(x => x.goods_stock, x => x.goods_stock - totalQuantity),
                        cancellationToken);

                if (affected == 0)
                {
                    throw new InvalidOperationException($"商品 {group.Key} 数据库库存不足");
                }

                foreach (var message in group)
                {
                    var orderNo = SeckillService.OrderNo(message.RunId, message.RequestId);
                    var remaining = bucketRemaining.GetValueOrDefault(
                        (message.RunId, message.ProductId, message.Bucket),
                        0);

                    await db.T1InventoryOrder.AddAsync(new T1InventoryOrder
                    {
                        order_no = orderNo,
                        sale_no = orderNo,
                        product_id = message.ProductId,
                        quantity = message.Quantity,
                        unit_price = price,
                        customer_name = "Seckill",
                        status = 1,
                        created_at = now
                    }, cancellationToken);

                    await db.T1InventoryStockFlow.AddAsync(new T1InventoryStockFlow
                    {
                        biz_no = orderNo,
                        product_id = message.ProductId,
                        product_name = product.goods_name,
                        quantity = message.Quantity,
                        remaining_stock = remaining,
                        source = "seckill",
                        created_at = now
                    }, cancellationToken);
                }
            }

            await db.SaveChangesAsync(cancellationToken);
            await uow.CompleteAsync();

            return newMessages.Count;
        }

        private async Task<Dictionary<(long, long, int), int>> ReadBucketRemainingAsync(
            List<SeckillOrderMessage> batch)
        {
            var keys = batch
                .Select(x => (x.RunId, x.ProductId, x.Bucket))
                .Distinct()
                .ToList();

            if (keys.Count == 0)
            {
                return new Dictionary<(long, long, int), int>();
            }

            var redis = _redis.GetDatabase();
            var redisKeys = keys
                .Select(x => (RedisKey)SeckillService.BucketKey(x.RunId, x.ProductId, x.Bucket))
                .ToArray();

            var values = await redis.StringGetAsync(redisKeys);
            var result = new Dictionary<(long, long, int), int>(keys.Count);

            for (var i = 0; i < keys.Count; i++)
            {
                result[keys[i]] = values[i].IsNull ? 0 : (int)values[i];
            }

            return result;
        }
    }
}
