using Abp.AspNetCore.Dependency;
using Abp.Dependency;

namespace t1_frame.webapi
{
    public class Program_Abp
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
               .UseCastleWindsor(IocManager.Instance.IocContainer);
    }
}
