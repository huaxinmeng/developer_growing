using Abp.Application.Services;
using Abp.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc;
using t1_frame.entityframeworkcore;

namespace t1_frame.webapi.Controllers
{
    [RemoteService]
    //[ApiController]
    //[Route("api/[controller]/[action]")]

    public class TestController : AbpController 
    {

        //private readonly T1FrameDbContext _t1FrameDbContext;
        //public TestController(T1FrameDbContext t1FrameDbContext)
        //{
        //    _t1FrameDbContext = t1FrameDbContext;
        //    Console.WriteLine($"{this.GetHashCode()}");
        //}

        

        [HttpGet]
        public IActionResult GetStr1(string name)
        {
            return Ok("12306");
        }
    }
}
