using Abp.AspNetCore;
using Abp.Castle.Logging.Log4Net;
using Abp.Dependency;
using Abp.EntityFrameworkCore;
using Castle.Facilities.Logging;
using Castle.MicroKernel.Registration;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Reflection;
using t1_frame.entityframeworkcore;
using t1_frame.common;
using t1_frame.entityframeworkcore.migrations;
namespace t1_frame.webapi
{
    public class Startup//  : IStartup
    {
        //public Startup(IConfiguration configuration, IWebHostEnvironment env)
        //{
        //    Configuration = configuration;
        //}

        private readonly IConfigurationRoot _appConfiguration;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public Startup(IWebHostEnvironment env)
        {
            _hostingEnvironment = env;
            _appConfiguration = env.GetAppConfiguration();
        }

        // public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //services.Configure<AbpAspNetCoreOptions>(options =>
            //{
            //    // 全局禁用包装
            //    options.DefaultWrapAttribute.IsEnabled = false;
            //    options.DefaultWrapAttribute.WrapOnSuccess = false;
            //    options.DefaultWrapAttribute.WrapOnError = false;
            //});
            //services.AddDbContext<T1FrameDbContext>(opt => opt.UseInMemoryDatabase("t1_frame"));

            // 使用 ABP 方式注册 DbContext
            services.AddAbpDbContext<T1FrameDbContext>(options =>
            {
                var connectionString = _appConfiguration.GetConnectionString("Default");
                // 配置默认数据库
                //options.DbContextOptions.UseInMemoryDatabase("t1_frame");
                options.DbContextOptions.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 27)));
                // options.AddDefaultRepositories();
            });

            services.AddAbpDbContext<MytestContext>(options =>
            {
                var connectionString = _appConfiguration.GetConnectionString("Default");
                // 配置默认数据库
                //options.DbContextOptions.UseInMemoryDatabase("t1_frame");
                options.DbContextOptions.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 27)));
                // options.AddDefaultRepositories();
            });

            services.AddControllers();//.AddControllersAsServices();
            //services.AddSwaggerGen();
            ConfigureSwagger(services);

            services.AddAbpWithoutCreatingServiceProvider<HostWebHostModule>(

               // Configure Log4Net logging
               options => {
        //           options.IocManager.IocContainer.Register(
        //    Component.For<T1FrameDbContext>()
        //        .UsingFactoryMethod(() =>
        //        {
        //            // 从内置 ServiceProvider 获取
        //            var serviceProvider = services.BuildServiceProvider();
        //            return serviceProvider.GetRequiredService<T1FrameDbContext>();
        //        })
        //        .LifestyleTransient()
        //);
                   options.IocManager.IocContainer.AddFacility<LoggingFacility>(
                   f => f.UseAbpLog4Net().WithConfig(_hostingEnvironment.IsDevelopment()
                       ? "log4net.config"
                       : "log4net.Production.config"
                   )
               );


           });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseAbp(options => { options.UseAbpRequestLocalization = false; }); // Initializes ABP framework.
            if (env == null || env.IsDevelopment())
            {
                //app.UseSwagger();
                //app.UseSwaggerUI();
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            // Enable middleware to serve generated Swagger as a JSON endpoint
            app.UseSwagger(c => { c.RouteTemplate = "swagger/{documentName}/swagger.json"; });

            // Enable middleware to serve swagger-ui assets (HTML, JS, CSS etc.)
            app.UseSwaggerUI(options =>
            {
                // specifying the Swagger JSON endpoint.
                options.SwaggerEndpoint($"/swagger/{"v1"}/swagger.json", $"selfProject API {"v1"}");
                //options.IndexStream = () => Assembly.GetExecutingAssembly()
                //    .GetManifestResourceStream("selfProject.Web.Host.wwwroot.swagger.ui.index.html");
                options.DisplayRequestDuration(); // Controls the display of the request duration (in milliseconds) for "Try it out" requests.
            }); // URL: /swagger

            // InitializeDatabase();
        }

        //IServiceProvider IStartup.ConfigureServices(IServiceCollection services)
        //{
        //    ConfigureServices(services);

        //    return services.BuildServiceProvider();
        //}

        //public void Configure(IApplicationBuilder app)
        //{
        //    Configure(app, null);
        //}

        private void ConfigureSwagger(IServiceCollection services)
        {
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "selfProject API",
                    Description = "selfProject",
                    // uncomment if needed TermsOfService = new Uri("https://example.com/terms"),
                    Contact = new OpenApiContact
                    {
                        Name = "selfProject",
                        Email = string.Empty,
                        Url = new Uri("https://twitter.com/aspboilerplate"),
                    },
                    License = new OpenApiLicense
                    {
                        Name = "MIT License",
                        Url = new Uri("https://github.com/aspnetboilerplate/aspnetboilerplate/blob/dev/LICENSE"),
                    }
                });
                options.DocInclusionPredicate((docName, description) => true);

                // Define the BearerAuth scheme that's in use
                options.AddSecurityDefinition("bearerAuth", new OpenApiSecurityScheme()
                {
                    Description =
                        "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey
                });

                //add summaries to swagger
                bool canShowSummaries = _appConfiguration.GetValue<bool>("Swagger:ShowSummaries");
                if (canShowSummaries)
                {
                    var hostXmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                    var hostXmlPath = Path.Combine(AppContext.BaseDirectory, hostXmlFile);
                    options.IncludeXmlComments(hostXmlPath);

                    //var applicationXml = $"selfProject.Application.xml";
                    //var applicationXmlPath = Path.Combine(AppContext.BaseDirectory, applicationXml);
                    //options.IncludeXmlComments(applicationXmlPath);

                    //var webCoreXmlFile = $"selfProject.Web.Core.xml";
                    //var webCoreXmlPath = Path.Combine(AppContext.BaseDirectory, webCoreXmlFile);
                    //options.IncludeXmlComments(webCoreXmlPath);
                }
            });
        }

        private void InitializeDatabase()
        {
            try
            {
                using (var scope = IocManager.Instance.Resolve<IScopedIocResolver>().CreateScope())
                {
                    var dbContext = scope.Resolve<IDbContextProvider<T1FrameDbContext>>().GetDbContext();
                    var configuration = scope.Resolve<IConfigurationRoot>();

                    Console.WriteLine("开始初始化数据库...");
                    Console.WriteLine($"连接字符串: {configuration.GetConnectionString("Default")}");

                    // 方法1：确保数据库创建（如果不存在）
                    var created = dbContext.Database.EnsureCreated();
                    Console.WriteLine($"数据库 {(created ? "已创建" : "已存在")}");

                    // 方法2：或者使用迁移
                    // ApplyMigrations(dbContext);

                    // 方法3：创建默认数据
                    // SeedDefaultData(dbContext);

                    Console.WriteLine("数据库初始化完成");
                }
            }
            catch (Exception ex)
            {
                // Logger.Error("数据库初始化失败", ex);
                Console.WriteLine($"数据库初始化失败: {ex.Message}");
                throw;
            }
        }
    }
}
