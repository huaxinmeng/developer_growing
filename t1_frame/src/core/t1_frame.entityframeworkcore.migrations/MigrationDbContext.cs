using Abp.Auditing;
using Abp.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using t1_frame.domain;
using Volo.Abp.AuditLogging;
using Volo.Abp.AuditLogging.EntityFrameworkCore;
//using Volo.Abp.AuditLogging;
//using Volo.Abp.AuditLogging.EntityFrameworkCore;

namespace t1_frame.entityframeworkcore.migrations
{
    public class MigrationDbContext : AbpDbContext, IAuditLoggingDbContext
    {
        public DbSet<T1ApiBase> T1ApiBase { get; set; }

        public DbSet<T1ApiAddress> T1ApiAddress { get; set; }

        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<AuditLogAction> AuditLogActions { get; set; }
        public DbSet<EntityChange> EntityChanges { get; set; }
        public DbSet<EntityPropertyChange> EntityPropertyChanges { get; set; }

        public MigrationDbContext(DbContextOptions<MigrationDbContext> options) : base(options)
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
            modelBuilder.ConfigureAuditLogging();
        }

        public async Task<int> SaveChangesOnDbContextAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        //public Task<int> SaveChangesOnDbContextAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        //{
        //    throw new NotImplementedException();
        //}

        //public Task<int> SaveChangesOnDbContextAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        //{
        //    throw new NotImplementedException();
        //}
    }
}