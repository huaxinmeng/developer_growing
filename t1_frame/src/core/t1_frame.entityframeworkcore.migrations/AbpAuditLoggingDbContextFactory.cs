using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.AuditLogging.EntityFrameworkCore;

namespace t1_frame.entityframeworkcore.migrations
{
    //public class AbpAuditLoggingDbContextFactory : IDesignTimeDbContextFactory<AuditLogDbContext>
    //{
    //    public AuditLogDbContext CreateDbContext(string[] args)
    //    {
    //        //var configuration = BuildConfiguration();
    //        //optionsBuilder.UseMySql("server=localhost;port=3306;user id=root;password=123456;database=t1_frame;Charset=utf8mb4;Allow User Variables=True;", new MySqlServerVersion(new Version(8, 0, 27)));
    //        var builder = new DbContextOptions<AuditLogDbContext>()
    //            .UseMySql("server=localhost;port=3306;user id=root;password=123456;database=t1_frame_mysql;Charset=utf8mb4;Allow User Variables=True;", new MySqlServerVersion(new Version(8, 0, 27)));

    //        return new AuditLogDbContext(builder.Options);
    //    }
    //}
}
