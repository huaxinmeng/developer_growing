using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace t1_frame.entityframeworkcore.migrations
{
    public class MigrationDbContextFactory : IDesignTimeDbContextFactory<MigrationDbContext>
    {
        public MigrationDbContext CreateDbContext(string[] args)
        {
            //var configuration = BuildConfiguration();
            //optionsBuilder.UseMySql("server=localhost;port=3306;user id=root;password=123456;database=t1_frame;Charset=utf8mb4;Allow User Variables=True;", new MySqlServerVersion(new Version(8, 0, 27)));
            var builder = new DbContextOptionsBuilder<MigrationDbContext>()
                .UseMySql("server=localhost;port=3306;user id=root;password=123456;database=t1_frame_mysql;Charset=utf8mb4;Allow User Variables=True;", new MySqlServerVersion(new Version(8, 0, 27)));

            return new MigrationDbContext(builder.Options);
        }

        //private static IConfigurationRoot BuildConfiguration()
        //{
        //    var builder = new ConfigurationBuilder()
        //        .SetBasePath(Directory.GetCurrentDirectory())
        //        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

        //    return builder.Build();
        //}
    }
}