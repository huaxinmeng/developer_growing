using Abp.AspNetCore;
using Abp.AspNetCore.Configuration;
using Abp.Extensions;
using Abp.Modules;
using Abp.Reflection.Extensions;
using System.Collections.Concurrent;
using t1_frame.application;
using t1_frame.entityframeworkcore;
using t1_frame.common;

namespace t1_frame.webapi
{
    [DependsOn(
        typeof(AbpAspNetCoreModule),
        typeof(HostEntityFrameworkModule),
        typeof(HostApplicationModule))]
    public class HostWebHostModule : AbpModule
    {
        private readonly IWebHostEnvironment _env;
        private readonly IConfigurationRoot _appConfiguration;

        public HostWebHostModule(IWebHostEnvironment env)
        {
            _env = env;
            _appConfiguration = env.GetAppConfiguration();
        }

        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(typeof(HostWebHostModule).GetAssembly());

            // 启用动态 API
            //Configuration.Modules.AbpAspNetCore()
            //    .CreateControllersForAppServices(
            //        typeof(HostWebHostModule).Assembly,
            //        moduleName: "app"
            //    );
        }
    }

    //public static class HostingEnvironmentExtensions
    //{
    //    public static IConfigurationRoot GetAppConfiguration(this IWebHostEnvironment env)
    //    {
    //        return AppConfigurations.Get(env.ContentRootPath, env.EnvironmentName, env.IsDevelopment());
    //    }
    //}

    //public static class AppConfigurations
    //{
    //    private static readonly ConcurrentDictionary<string, IConfigurationRoot> _configurationCache;

    //    static AppConfigurations()
    //    {
    //        _configurationCache = new ConcurrentDictionary<string, IConfigurationRoot>();
    //    }

    //    public static IConfigurationRoot Get(string path, string environmentName = null, bool addUserSecrets = false)
    //    {
    //        var cacheKey = path + "#" + environmentName + "#" + addUserSecrets;
    //        return _configurationCache.GetOrAdd(
    //            cacheKey,
    //            _ => BuildConfiguration(path, environmentName, addUserSecrets)
    //        );
    //    }

    //    private static IConfigurationRoot BuildConfiguration(string path, string environmentName = null, bool addUserSecrets = false)
    //    {
    //        var builder = new ConfigurationBuilder()
    //            .SetBasePath(path)
    //            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

    //        if (!environmentName.IsNullOrWhiteSpace())
    //        {
    //            builder = builder.AddJsonFile($"appsettings.{environmentName}.json", optional: true);
    //        }

    //        builder = builder.AddEnvironmentVariables();

    //        if (addUserSecrets)
    //        {
    //            builder.AddUserSecrets(typeof(AppConfigurations).GetAssembly(), optional: true);
    //        }

    //        return builder.Build();
    //    }
    //}
}
