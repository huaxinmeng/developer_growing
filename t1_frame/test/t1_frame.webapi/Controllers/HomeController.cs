using Microsoft.AspNetCore.Mvc;

namespace t1_frame.webapi.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class HomeController : ControllerBase
    {
        [HttpGet]
        public string GetStr()
        {
            return "Get 369";
        }

        [HttpPost]
        public string Test(TestCondition condition)
        {
            return "Post 369";
        }
    }
}