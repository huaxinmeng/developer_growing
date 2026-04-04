using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using t1_frame.core.abp;
using t1_frame.entityframeworkcore.abp;
using t1_frame.response.abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Caching;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.ObjectMapping;

namespace t1_frame.application.abp
{
    public class GoodsAppService : ApplicationService, IGoodsAppService
    {
        private readonly IRepository<T1GoodsStock, long> _repository;
        private readonly IDistributedCache<string> _cache;
        private readonly IDatabase _db;

        public GoodsAppService(IRepository<T1GoodsStock, long> repository,
             IDistributedCache<string> cache,
            IConnectionMultiplexer redis)
        {
            _repository = repository;
            _cache = cache;
            _db = redis.GetDatabase();
        }

        public async virtual Task<bool> AddGoods(GoodsInput input)
        {
            var product = await _repository.FirstOrDefaultAsync(t => t.goods_name == input.goods_name);
            if (product == null)
            {
                var result = ObjectMapper.Map<GoodsInput, T1GoodsStock>(input);
                await _repository.InsertAsync(result);
            }
            else
            {
                product.goods_stock += input.goods_stock;
            }
                
            await _db.StringSetAsync($"product:{input.goods_name}:stock", (product?.goods_stock ?? input.goods_stock).ToString());
            return true;
        }

        public long GetCount()
        {
            return CounterHelper.Instance.GetCount();
        }
    }
}
