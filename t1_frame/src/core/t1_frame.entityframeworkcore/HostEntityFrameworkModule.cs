using Abp.Dapper;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.EntityFrameworkCore;
using Abp.EntityFrameworkCore.Configuration;
using Abp.EntityFrameworkCore.Repositories;
using Abp.Modules;
using Abp.Reflection.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace t1_frame.entityframeworkcore
{
    [DependsOn(typeof(AbpEntityFrameworkCoreModule), typeof(AbpDapperModule))]
    public class HostEntityFrameworkModule : AbpModule
    {
        //public override void ConfigureServices(ServiceConfigurationContext context)
        //{
        //    var services = context.Services;

        //    // 使用 ABP 方式注册 DbContext
        //    services.AddAbpDbContext<T1FrameDbContext>(options =>
        //    {
        //        // 配置默认数据库
        //        options.DbContextOptions.UseInMemoryDatabase("t1_frame");
        //    });
        //}

        public override void PreInitialize()
        {
            // 最关键的一行！
            // Configuration.DefaultNameOrConnectionString = "Default";

            // 简单直接的 DbContext 注册
            //Configuration.Modules.AbpEfCore().AddDbContext<T1FrameDbContext>(options =>
            //{
            //    // 直接配置，不检查 ExistingConnection
            //    //if (options.ExistingConnection != null)
            //    //{
            //    //    options.DbContextOptions.UseInMemoryDatabase("t1_frame");
            //    //}
            //    //else
            //    //{
            //    //    options.DbContextOptions.UseInMemoryDatabase("t1_frame");
            //    //}

            //    options.DbContextOptions.UseInMemoryDatabase("t1_frame");
            //});

            // 禁用事务
            Configuration.UnitOfWork.IsTransactional = false;
        }

        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(typeof(HostEntityFrameworkModule).GetAssembly());

            //Configuration.Modules.AbpEfCore().AddDbContext<T1FrameDbContext>(options =>
            //{
            //    options.DbContextOptions.UseInMemoryDatabase("t1_frame");
            //});

            //IocManager.Register(typeof(IRepository<,>), typeof(EfCoreRepositoryBase<,>),
            //        DependencyLifeStyle.Transient);
        }

        public override void PostInitialize()
        {
            // InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            try
            {
                using (var scope = IocManager.Resolve<IScopedIocResolver>().CreateScope())
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
                Logger.Error("数据库初始化失败", ex);
                Console.WriteLine($"数据库初始化失败: {ex.Message}");
                throw;
            }
        }
    }
}
