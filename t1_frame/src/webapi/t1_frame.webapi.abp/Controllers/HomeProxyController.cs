using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace t1_frame.webapi.abp.Controllers
{
    [RemoteService]
    public class HomeProxyController : AbpController
    {
        [HttpGet]
        public IActionResult GetStr1(string name)
        {
            return Ok("1230626");
        }
    }
}
