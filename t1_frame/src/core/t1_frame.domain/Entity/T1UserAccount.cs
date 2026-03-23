using Abp.Domain.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace t1_frame.domain
{
    [Table("t1_user_account")]
    public class T1UserAccount : Entity<long>
    {
        public long user_id {  get; set; }

        public decimal amount {  get; set; }
    }
}
