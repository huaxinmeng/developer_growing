using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.AutoMapper;
using Volo.Abp.Domain.Entities.Events.Distributed;
using Volo.Abp.FluentValidation;
using Volo.Abp.Modularity;

namespace t1_frame.response.abp
{
    [DependsOn(
    typeof(AbpFluentValidationModule), //Add the FluentValidation module
        typeof(AbpAutoMapperModule)
    )]
    public class HostResModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<AbpAutoMapperOptions>(options =>
            {
                //Add all mappings defined in the assembly of the MyModule class
                options.AddMaps<HostResModule>();
            });

            Configure<AbpDistributedEntityEventOptions>(options =>
            {
                //Enable for all entities
                // options.AutoEventSelectors.Add<GoodsRaceEto>();
            });
        }
    }
}
