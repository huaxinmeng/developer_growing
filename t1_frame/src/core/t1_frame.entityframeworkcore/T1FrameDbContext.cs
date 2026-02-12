using Abp.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using t1_frame.domain;

namespace t1_frame.entityframeworkcore
{
    public class T1FrameDbContext : AbpDbContext
    {
        public DbSet<T1ApiBase> T1ApiBase { get; set; }

        public DbSet<T1ApiAddress> T1ApiAddress { get; set; }

        public T1FrameDbContext(DbContextOptions<T1FrameDbContext> options) : base(options)
        {
        }

        //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        //{
        //    optionsBuilder.UseMySql("server=localhost;port=3306;user id=root;password=123456;database=t1_frame;Charset=utf8mb4;Allow User Variables=True;", new MySqlServerVersion(new Version(8, 0, 27)));
        //}

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<T1ApiBase>().HasKey(b => b.Id);
            modelBuilder.Entity<T1ApiAddress>().HasKey(b => b.Id);
            base.OnModelCreating(modelBuilder);
        }
    }
}