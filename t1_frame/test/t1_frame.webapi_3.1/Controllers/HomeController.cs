//using Microsoft.AspNetCore.Components;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace t1_frame.webapi_3._1.Controllers
{
    /// <summary>
    /// 三种依赖注入方式
    /// 1.构造器注入
    /// 2.属性注入
    /// 3.方法注入
    /// </summary>
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class HomeController : ControllerBase
    {
        private readonly HomeService service;

        [Microsoft.AspNetCore.Components.Inject] //常规应用不支持
        public HomeService service1 { get; set; }

        private readonly IBackgroundJobClient _backgroundJobs;
        public HomeController(HomeService service, IBackgroundJobClient backgroundJobs)
        {
            this.service = service;
            _backgroundJobs = backgroundJobs;
        }

        [HttpGet]
        public string GetStr([FromServices] HomeService service2)
        {
            return service2.GetStr();
        }

        [HttpGet]
        public string GetStr0([FromServices] IFoo foo, [FromServices] IEnumerable<IBar> bars, [FromServices] IBaz baz, [FromServices] IGux gux)
        {
            return $"{foo.GetType().Name} {foo.GetHashCode()} - {bars.GetType().Name} {bars.GetHashCode()} - {baz.GetType().Name} {baz.GetHashCode()} - {gux.GetType().Name} {gux.Foo.GetHashCode()} {gux.Bar.GetHashCode()} {gux.Baz.GetHashCode()}";
        }

        [HttpGet]
        public string GetStr1([FromServices] IFoobar<IFoo, IBar> item1, [FromServices] IFoobar<IFoo, IBaz> item2, [FromServices] IFoobar<IBaz, IBar> item3)
        {
            return $"{item1.Foo.GetType().Name} - {item2.Bar.GetType().Name} - {item3.Bar.GetType().Name}";
        }

        [HttpGet]
        public string GetStr2([FromServices] IFoo foo, [FromServices] IBaz baz, [FromServices] IGux gux)
        {
            return $"{foo.GetType().Name} {foo.GetHashCode()}  - {baz.GetType().Name} {baz.GetHashCode()} - {gux.GetType().Name} {gux.Foo.GetHashCode()} {gux.Bar.GetHashCode()} {gux.Baz.GetHashCode()}";
        }

        [HttpGet]
        public string HangfireTest()
        {
            RecurringJob.AddOrUpdate("easyjob", () => Console.Write("Easy!"), Cron.Minutely());
            //_backgroundJobs.Schedule(() => Console.WriteLine("Hello, world"), TimeSpan.FromSeconds(10));
            return _backgroundJobs.Enqueue(() => Console.WriteLine($"time: {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} Hello, world!"));
        }
    }
}
