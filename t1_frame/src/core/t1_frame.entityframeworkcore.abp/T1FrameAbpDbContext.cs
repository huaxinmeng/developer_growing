using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.AuditLogging;
using Volo.Abp.AuditLogging.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace t1_frame.entityframeworkcore.abp
{
    public class T1FrameAbpDbContext : AbpDbContext<T1FrameAbpDbContext>, IAuditLoggingDbContext
    {
        public DbSet<T1ApiBase> T1ApiBase { get; set; }

        public DbSet<T1ApiAddress> T1ApiAddress { get; set; }

        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<AuditLogAction> AuditLogActions { get; set; }
        public DbSet<EntityChange> EntityChanges { get; set; }
        public DbSet<EntityPropertyChange> EntityPropertyChanges { get; set; }

        public DbSet<T1User> T1User { get; set; }
        public DbSet<T1UserAccount> T1UserAccount { get; set; }
        public DbSet<T1GoodsStock> T1GoodsStock { get; set; }
        public DbSet<T1TradeLog> T1TradeLog { get; set; }


        public T1FrameAbpDbContext(DbContextOptions<T1FrameAbpDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<T1ApiBase>().HasKey(b => b.Id);
            modelBuilder.Entity<T1ApiAddress>().HasKey(b => b.Id);

            modelBuilder.Entity<T1User>().HasKey(b => b.Id);
            modelBuilder.Entity<T1UserAccount>(entity =>
            {
                entity.HasKey(e => e.Id);

                // 配置 decimal：总长度 18，小数位 2（即 DECIMAL(18,2)）
                entity.Property(e => e.amount)
                    .HasPrecision(18, 4)  // 精度 18，小数位 2
                    .IsRequired();
            });
            modelBuilder.Entity<T1GoodsStock>().HasKey(b => b.Id);
            modelBuilder.Entity<T1TradeLog>().HasKey(b => b.Id);

            modelBuilder.Entity<T1UserAccount>().Property(e => e.version).IsConcurrencyToken();  // EF Core 乐观锁

            base.OnModelCreating(modelBuilder);

            modelBuilder.ConfigureAuditLogging();
        }
    }
}
