using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.AspNetCore.Auditing;
using Volo.Abp.Auditing;
using Volo.Abp.AuditLogging;
using Volo.Abp.AuditLogging.EntityFrameworkCore;
using Volo.Abp.AuditLogging.MongoDB;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories.MongoDB;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Modularity;
using Volo.Abp.MongoDB;
using Volo.Abp.Uow;

namespace t1_frame.entityframeworkcore.abp
{
    [DependsOn(typeof(AbpEntityFrameworkCoreModule),
        // typeof(AbpAuditLoggingMongoDbModule)//,
        typeof(AbpMongoDbModule),
       typeof(AbpAuditLoggingEntityFrameworkCoreModule)
        )]
    public class HostEntityFrameworkModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<AbpAuditingOptions>(options =>
            {
                options.EntityHistorySelectors.AddAllEntities();
            });

            //Configure<AbpAspNetCoreAuditingOptions AbpAuditLoggingOptions>(options =>
            //{
            //    // 明确告诉 ABP：审计日志应该使用 T1FrameAbpDbContext 来存储
            //    options.EntityHistoryStoreDbContextType = typeof(T1FrameAbpDbContext);
            //});

            context.Services.AddAbpDbContext<T1FrameAbpDbContext>(options =>
            {
                options.AddDefaultRepositories(includeAllEntities: true);
            });

            context.Services.AddAbpDbContext<MytestAbpContext>(options =>
            {
                options.AddDefaultRepositories(includeAllEntities: true);
            });

            context.Services.AddMongoDbContext<T1FrameMongoDbContext>(options =>
            {
                options.AddDefaultRepositories(includeAllEntities: true);
                options.AddRepository<Message, MessageRepository>();
            });

            context.Services.AddAbpDbContext<AbpAuditLoggingDbContext>(options =>
            {
                options.AddDefaultRepositories(includeAllEntities: true);
            });

            //Configure<AbpUnitOfWorkDefaultOptions>(options =>
            //{
            //    options.TransactionBehavior = UnitOfWorkTransactionBehavior.Enabled;
            //});

            Configure<AbpDbConnectionOptions>(options =>
            {
                options.Databases.Configure("t1_frame", db =>
                {
                    db.MappedConnections.Add("mongodb");
                    // db.MappedConnections.Add(AbpAuditLoggingDbProperties.ConnectionStringName);
                });
                options.Databases.Configure("t1_frame_mysql", db =>
                {
                    db.MappedConnections.Add("Default");
                    db.MappedConnections.Add(AbpAuditLoggingDbProperties.ConnectionStringName);
                });
            });

            var appConfiguration = context.Services.GetConfiguration();
            Configure<AbpDbContextOptions>(options =>
            {          
                // Customized configuration for a specific DbContext
                options.Configure<T1FrameAbpDbContext>(opts =>
                {
                    var connectionString = appConfiguration.GetConnectionString("Default");
                    // 配置默认数据库
                    //options.DbContextOptions.UseInMemoryDatabase("t1_frame");
                    opts.DbContextOptions.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 27)));
                });

                options.Configure<MytestAbpContext>(opts =>
                {
                    var connectionString = appConfiguration.GetConnectionString("Default");
                    // 配置默认数据库
                    //options.DbContextOptions.UseInMemoryDatabase("t1_frame");
                    opts.DbContextOptions.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 27)));
                });
                //mongodb
                //options.Configure<T1FrameMongoDbContext>(opts =>
                //{
                //    var connectionString = appConfiguration.GetConnectionString("mongodb");
                //    // 配置默认数据库
                //    //options.DbContextOptions.UseInMemoryDatabase("t1_frame");
                //    opts.DbContextOptions.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 27)));
                //});

                options.Configure<AbpAuditLoggingDbContext>(opts =>
                {
                    var connectionString = appConfiguration.GetConnectionString(AbpAuditLoggingDbProperties.ConnectionStringName);
                    // 配置默认数据库
                    //options.DbContextOptions.UseInMemoryDatabase("t1_frame");
                    opts.DbContextOptions.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
                });
            });
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            
        }
    }
}
