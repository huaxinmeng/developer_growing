using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.EntityFrameworkCore;

namespace t1_frame.entityframeworkcore.abp
{
    public partial class MytestAbpContext : AbpDbContext<MytestAbpContext>
    {
        public MytestAbpContext(DbContextOptions<MytestAbpContext> options) : base(options)
        {
        }

        public virtual DbSet<Student> Student { get; set; }

        //    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        //#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        //        => optionsBuilder.UseMySql("server=localhost;port=3306;user id=root;password=123456;database=mytest;charset=utf8mb4;allow user variables=True", Microsoft.EntityFrameworkCore.ServerVersion.Parse("8.0.27-mysql"));

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //modelBuilder
            //    .UseCollation("utf8_general_ci")
            //    .HasCharSet("utf8");

            modelBuilder.Entity<Student>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("PRIMARY");

                entity.ToTable("student");

                entity.HasIndex(e => e.Sno, "sno").IsUnique();

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Cost)
                    .HasComment("花费")
                    .HasColumnName("cost");
                entity.Property(e => e.CreateBy)
                    .HasComment("创建人")
                    .HasColumnName("create_by");
                entity.Property(e => e.CreateTime)
                    .HasColumnType("datetime")
                    .HasColumnName("create_time");
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasColumnName("name");
                entity.Property(e => e.Sex)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasColumnName("sex");
                entity.Property(e => e.Sno)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasColumnName("sno");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
