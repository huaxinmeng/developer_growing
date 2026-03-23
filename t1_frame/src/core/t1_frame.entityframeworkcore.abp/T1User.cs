using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Domain.Entities;

namespace t1_frame.entityframeworkcore.abp
{
    [Table("t1_user")]
    public class T1User : Entity<long>
    {
        /// <summary>
        /// 用户编码
        /// </summary>
        [MaxLength(32)]
        public string user_code {  get; set; }

        /// <summary>
        /// 用户名称
        /// </summary>
        [MaxLength(100)]
        public string user_name { get; set; }
    }
}
