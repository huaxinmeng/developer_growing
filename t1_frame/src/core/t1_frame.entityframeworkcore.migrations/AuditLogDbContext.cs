using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.AuditLogging.EntityFrameworkCore;

namespace t1_frame.entityframeworkcore.migrations
{
    public class AuditLogDbContext : AbpAuditLoggingDbContext
    {
        public AuditLogDbContext(DbContextOptions<AbpAuditLoggingDbContext> options) : base(options)
        {
        }
    }
}
