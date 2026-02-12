using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.EntityFrameworkCore;

namespace t1_frame.entityframeworkcore.abp
{
    public class T1FrameAbpDbContext : AbpDbContext<T1FrameAbpDbContext>
    {
        public DbSet<T1ApiBase> T1ApiBase { get; set; }

        public DbSet<T1ApiAddress> T1ApiAddress { get; set; }

        public T1FrameAbpDbContext(DbContextOptions<T1FrameAbpDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<T1ApiBase>().HasKey(b => b.Id);
            modelBuilder.Entity<T1ApiAddress>().HasKey(b => b.Id);
            base.OnModelCreating(modelBuilder);
        }
    }
}
