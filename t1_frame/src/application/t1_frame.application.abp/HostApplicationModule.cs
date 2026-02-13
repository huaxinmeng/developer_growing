using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using t1_frame.response.abp;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Modularity;

namespace t1_frame.application.abp
{
    [DependsOn(typeof(HostResModule))]
    public class HostApplicationModule : AbpModule
    {
        public override void PreConfigureServices(ServiceConfigurationContext context)
        {
            Configure<AbpAspNetCoreMvcOptions>(options =>
            {
                options.ConventionalControllers
                    .Create(typeof(HostApplicationModule).Assembly, opts =>
                    {
                        opts.RootPath = "app";
                    });
            });
        }
    }
}
