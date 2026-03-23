using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using t1_frame.entityframeworkcore.abp;
using t1_frame.response.abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.ObjectMapping;

namespace t1_frame.application.abp
{
    public class GoodsAppService : ApplicationService, IGoodsAppService
    {
        private readonly IRepository<T1GoodsStock, long> _repository;
        public async virtual Task<bool> AddGoods(GoodsInput input)
        {
            var result = ObjectMapper.Map<GoodsInput, T1GoodsStock>(input);
            await _repository.InsertAsync(result);

            return true;
        }
    }
}
