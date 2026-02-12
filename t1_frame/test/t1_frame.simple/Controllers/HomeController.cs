using Microsoft.AspNetCore.Mvc;

namespace t1_frame.simple.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class HomeController : ControllerBase
    {
        [HttpGet]
        public int GetById(int id)
        {
            return id * 100;
        }
    }
}
