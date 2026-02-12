

using Microsoft.AspNetCore;
using Microsoft.EntityFrameworkCore;
using t1_frame.entityframeworkcore;

namespace t1_frame.webapi
{
    public class Program
    {
        public static void Main2(string[] args)
        {
           new AspNetHosting().ConfigureWebHost().Run();
        }

        public class AspNetHosting : IStartup
        {
            public IWebHost ConfigureWebHost()
            {

                var builder = new WebHostBuilder();
                builder.UseKestrel();
                // builder.UseStartup<Startup>();
                builder.ConfigureServices((context, services) =>
                {
                    services.AddSingleton<IStartup>(this);
                });

                return builder.Build();
                // app.Run();
            }

            void IStartup.Configure(IApplicationBuilder app)
            {
                app.UseSwagger();
                app.UseSwaggerUI();
                app.UseDeveloperExceptionPage();


                app.UseRouting();

                app.UseAuthorization();

                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                });
            }

            IServiceProvider IStartup.ConfigureServices(IServiceCollection services)
            {
                services.AddDbContext<T1FrameDbContext>(opt => opt.UseInMemoryDatabase("t1_frame"));
                // services.AddDatabaseDeveloperPageExceptionFilter();
                services.AddControllers();
                services.AddSwaggerGen();


                return services.BuildServiceProvider();
            }
        }
    }
}
