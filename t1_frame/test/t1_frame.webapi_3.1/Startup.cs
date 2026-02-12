using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz.AspNetCore;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace t1_frame.webapi_3._1
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<HomeService>();
            services.AddScoped<IFoo, Foo>();
            services.AddTransient<IGux, Gux>();
            services.AddSingleton<IBar>(new Bar());
            //services.AddTransient<IBar, Bar>();
            //services.AddTransient<IBar, Bar>();
            services.AddTransient<IBaz>(_ => new Baz());
            services.AddTransient(typeof(IFoobar<,>), typeof(Foobar<,>));

            services.AddTransient(typeof(IBar), typeof(BarA));

            // Add Hangfire services.
            services.AddHangfire(configuration => configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(Configuration.GetConnectionString("HangfireConnection")));

            // Add the processing server as IHostedService
            services.AddHangfireServer();

            services.AddQuartz(q =>
            {
                // base Quartz scheduler, job and trigger configuration
                // Just use the name of your job that you created in the Jobs folder.
                var jobKey = new JobKey("SendEmailJob");
                q.AddJob<SendEmailJob>(opts => opts.WithIdentity(jobKey));

                q.AddTrigger(opts => opts
                    .ForJob(jobKey)
                    .WithIdentity("SendEmailJob-trigger")
                    //This Cron interval can be described as "run every minute" (when second is zero)
                    .WithCronSchedule("0/2 * * ? * *")
                );
            });

            // ASP.NET Core hosting
            services.AddQuartzServer(options =>
            {
                // when shutting down we want jobs to complete gracefully
                options.WaitForJobsToComplete = true;
            });

            services.AddControllers();
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IBackgroundJobClient backgroundJobs, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHangfireDashboard();
            });

            backgroundJobs.Enqueue(() => Console.WriteLine("Hello world from Hangfire!"));
        }
    }
}
