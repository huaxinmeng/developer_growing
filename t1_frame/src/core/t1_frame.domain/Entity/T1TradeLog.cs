using Abp.Domain.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace t1_frame.domain
{
    [Table("t1_trade_log")]
    public class T1TradeLog : Entity<long>
    {
        public long user_id { get; set; }

        [MaxLength(50)]
        public string source {  get; set; }

        [MaxLength(512)]
        public string description { get; set; }
    }
}
