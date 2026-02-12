using Microsoft.AspNetCore.Mvc;

namespace t1_frame.webapi.abp.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class TestProxyController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetStr1(string name)
        {
            return Ok("12306");
        }
    }
}
