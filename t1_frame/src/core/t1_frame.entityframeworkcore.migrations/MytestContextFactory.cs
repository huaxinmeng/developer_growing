using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace t1_frame.entityframeworkcore.migrations
{
    public class MytestContextFactory : IDesignTimeDbContextFactory<MytestMigrationContext>
    {
        public MytestMigrationContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<MytestMigrationContext>()
               .UseMySql("server=localhost;port=3306;user id=root;password=123456;database=t1_frame_mysql;Charset=utf8mb4;Allow User Variables=True;", new MySqlServerVersion(new Version(8, 0, 27)));

            return new MytestMigrationContext(builder.Options);
        }
    }
}
