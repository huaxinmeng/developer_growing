using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using t1_frame.entityframeworkcore.abp;
using t1_frame.response.abp;
using Volo.Abp;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Uow;

namespace t1_frame.application.abp
{
    public class GoodsRaceHandler : IDistributedEventHandler<GoodsRaceActEto>, ITransientDependency
    {
        private readonly IRepository<T1GoodsStock, long> _stockRepository;
        private readonly IRepository<T1TradeLog, long> _logRepository;
        private readonly IDistributedCache<string> _cache;
        public GoodsRaceHandler(IRepository<T1GoodsStock, long> stockRepository,
            IRepository<T1TradeLog, long> logRepository,
            IDistributedCache<string> cache)
        {
            _stockRepository = stockRepository;
            _logRepository = logRepository;
            _cache = cache;
        }

        // [UnitOfWork]
        public async Task HandleEventAsync(GoodsRaceActEto eventData)
        {
            //await Task.Delay(100);
            //Console.WriteLine($"{JsonConvert.SerializeObject(eventData)}");

            // 生成幂等键
            var idempotentKey = $"idempotent:{eventData.request_id}";
            var lockValue = Guid.NewGuid().ToString("N");
            var lockAcquired = await _cache.GetOrAddAsync(
               idempotentKey,
               async () => await Task.FromResult(lockValue),
               () => new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) }
            );

            if (lockAcquired != lockValue)
            {
                Console.WriteLine($"重复消息被过滤: {idempotentKey}");
                return;
            }


            T1User user = null;
            /*
            var product = await _stockRepository.FirstOrDefaultAsync(t => t.goods_name == eventData.goods_name);
            if (product == null)
            {
                throw new AbpException($"商品{eventData.goods_name}未上架...");
            }
            else if (product.goods_stock < eventData.goods_stock)
            {
                throw new AbpException($"商品{eventData.goods_name}库存不足{product.goods_stock}...");
            }

            product.goods_stock -= eventData.goods_stock;
            */

            FormattableString sql = $@"
                                        UPDATE t1_goods_stock 
                                        SET goods_stock = goods_stock - {eventData.goods_stock}
                                        WHERE goods_name = {eventData.goods_name} AND goods_stock >= {eventData.goods_stock}";

            try
            {
                var rowsAffected = await (await _stockRepository
                .GetDbContextAsync()).Database
                .ExecuteSqlInterpolatedAsync(sql);

                if (rowsAffected == 0)
                {
                    // 可能是库存不足或商品记录不存在
                    throw new AbpException("扣减失败，库存不足或商品记录不存在");
                }

                await _logRepository.InsertAsync(new T1TradeLog
                {
                    user_id = user?.Id ?? 0,
                    source = "Stock",
                    description = $"{user?.user_name ?? eventData.user_code} 扣减 {eventData.goods_stock}"
                });
            }
            catch
            {
                // 失败时删除幂等键，允许重试
                await _cache.RemoveAsync(idempotentKey);
                throw;
            }
        }
    }
}
