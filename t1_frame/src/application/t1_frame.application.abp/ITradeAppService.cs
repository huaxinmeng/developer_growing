using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using t1_frame.response.abp;
using Volo.Abp.Application.Services;

namespace t1_frame.application.abp
{
    public interface ITradeAppService : IApplicationService
    {
        Task<bool> Deduct(DeductInput input);

        Task<bool> DeductWithRedis(DeductInput input);

        Task<bool> DeductWithLua(DeductInput input);

        Task<bool> EventBusTest(GoodsRaceEto input);
    }
}
