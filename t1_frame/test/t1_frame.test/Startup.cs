using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace t1_frame.Test
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);
            });

            //DbProviderFactories.RegisterFactory("MySql.Data.MySqlClient", MySqlClientFactory.Instance);

            //服务层注册
            //services.AddScoped<IUnitOfWork, UnitOfWork>();
            //services.AddScoped<IDbProviderConfig, DbProviderConfig>();
            //services.AddScoped<ISiteErrorMessageResolver>();

            services.AddHttpClient();
            //集中注册服务
            //Resolve.ResolveClass(services);
            //ConfiguredMappingConfig();
            services.AddMemoryCache();
            services.AddTransient(typeof(Lazy<>));//注册Lazy
            //Hosting.SiteErrors.Initialize(provider);
            //services.AddMvc()
            //    .AddXmlSerializerFormatters();

            //services.AddAuthentication(options =>
            //{
            //    options.DefaultScheme = "Cookies";
            //}).AddCookie();

            //Configure Abp and Dependency Injection
            //return services.AddAbp<AppModule>(options =>
            //{
            //    //Test setup
            //    options.SetupTest();
            //});
        }

        //private void SetFeiShuNotifyPrivateKey(IServiceProvider serviceProvider)
        //{
        //    var assembly = GetType().Assembly;
        //    var sourceName = assembly.GetManifestResourceNames().FirstOrDefault(t=>t.EndsWith("private_key.pem"));
        //    if (sourceName == null) return;
        //    var cache = serviceProvider.GetService<IMemoryCache>();
        //    if(cache == null) return;
        //    using (Stream stream = assembly.GetManifestResourceStream(sourceName))
        //    {
        //        using (StreamReader reader = new StreamReader(stream))
        //        {
        //            var privateKey = reader.ReadToEnd();
        //            privateKey = privateKey.Replace("-----BEGIN RSA PRIVATE KEY-----", "").Replace("-----END RSA PRIVATE KEY-----", "").Replace("\r", "").Replace("\n", "").Trim();
        //            privateKey = privateKey.Replace("-----BEGIN PRIVATE KEY-----", "").Replace("-----END PRIVATE KEY-----", "").Replace("\r", "").Replace("\n", "").Trim();
        //            cache.Set(AppCacheEnum.FeiShuRequestPrivateKey.ToString(), privateKey);
        //        }
        //    }
        //}

        //private void ConfiguredMappingConfig()
        //{
        //    //MapsterHelper.SetTypeAdapterConfig(new List<(Type source, Type destination)>() {
        //    //    (typeof(DapApiBaseEntity), typeof(GetApiBaseResponse)),
        //    //    (typeof(DapApiBaseEntity), typeof(SeachApiBaseResponse)),
        //    //    (typeof(DapApiBaseEntity), typeof(PopupSeachApiBaseResponse)),
        //    //    (typeof(DapApiBaseEntity), typeof(AddOrUpdateApiBaseCondition)),
        //    //    (typeof(AddOrUpdateApiBaseCondition), typeof(DapApiBaseEntity))});

        //    var autoMapperCollectionAssembly = Assembly.Load("Nebula.DAP.Server.Entities");
        //    MapsterHelper.SetTypeAdapterConfig(autoMapperCollectionAssembly);
        //}

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            //SetFeiShuNotifyPrivateKey(app.ApplicationServices);
            //app.UseAbp(); //Initializes ABP framework.

            app.UseRouting();

            //app.UseAuthentication();
            //app.UseAbpAuthorizationExceptionHandling();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute("defaultWithArea", "{area}/{controller=Home}/{action=Index}/{id?}");
                endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");

                //app.ApplicationServices.GetRequiredService<IAbpAspNetCoreConfiguration>()
                //                        .EndpointConfiguration
                //                        .ConfigureAllEndpoints(endpoints);

                //endpoints.MapControllers();
            });
        }
    }
}