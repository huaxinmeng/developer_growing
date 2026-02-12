using Abp.Application.Services;
using Abp.AspNetCore.Configuration;
using Abp.Dependency;
using Abp.Modules;
using Abp.Reflection.Extensions;
using Abp.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace t1_frame.application
{
    public class HostApplicationModule : AbpModule
    {
        public override void Initialize()
        {
           IocManager.RegisterAssemblyByConvention(typeof(HostApplicationModule).GetAssembly());

            // 配置自动 API 控制器
            Configuration.Modules.AbpAspNetCore()
                .CreateControllersForAppServices(
                    typeof(HostApplicationModule).Assembly,
                    moduleName: "app",
                    useConventionalHttpVerbs: true
                );
                //.ConfigureControllerModel(model =>
                //{
                //    model.Attributes.Append(new DontWrapResultAttribute());
                //});
                //.ConfigureControllerModel(model =>
                //{
                //    // 为特定控制器禁用包装
                //    model.Filters.Add(new DontWrapResultAttribute());
                //});
        }
    }
}
