namespace t1_frame.webapi.abp
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services) => services.AddApplication<HostWebHostModule>();

        public void Configure(IApplicationBuilder app) => app.InitializeApplication();
    }
}
