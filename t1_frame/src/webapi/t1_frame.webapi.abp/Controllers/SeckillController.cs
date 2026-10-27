using Microsoft.AspNetCore.Mvc;
using t1_frame.application.abp.Seckill;
using Volo.Abp.Auditing;
using Volo.Abp.Uow;

namespace t1_frame.webapi.abp.Controllers
{
    /// <summary>
    /// 1000 QPS 秒杀入口：只访问 Redis + 内存队列，不访问 MySQL。
    /// </summary>
    [ApiController]
    [Route("api/seckill")]
    [DisableAuditing]
    [UnitOfWork(IsDisabled = true)]
    public class SeckillController : ControllerBase
    {
        private readonly SeckillService _seckillService;

        public SeckillController(SeckillService seckillService)
        {
            _seckillService = seckillService;
        }

        [HttpPost("{productId:long}")]
        public Task<SeckillResultDto> SeckillAsync(
            long productId,
            [FromQuery] int quantity = 1,
            [FromQuery] string? requestId = null)
        {
            return _seckillService.SeckillAsync(productId, quantity, requestId);
        }

        [HttpGet("status/{requestId}")]
        public Task<SeckillStatusDto> GetStatusAsync(string requestId)
        {
            return _seckillService.GetStatusAsync(requestId);
        }

        [HttpGet("stats")]
        public Task<SeckillStatsDto> GetStatsAsync([FromQuery] long productId = 2)
        {
            return _seckillService.GetStatsAsync(productId);
        }

        [HttpPost("reset")]
        public Task<SeckillResetDto> ResetAsync(
            [FromQuery] long productId = 2,
            [FromQuery] int stock = 10000)
        {
            return _seckillService.ResetAsync(productId, stock);
        }
    }
}
