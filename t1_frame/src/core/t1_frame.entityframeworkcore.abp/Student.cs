using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Domain.Entities;

namespace t1_frame.entityframeworkcore.abp
{
    public partial class Student : Entity<int>
    {
        // public int Id { get; set; }

        public string Name { get; set; } = "";

        public string Sex { get; set; } = "";

        public string Sno { get; set; } = "";

        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 创建人
        /// </summary>
        public int CreateBy { get; set; }

        /// <summary>
        /// 花费
        /// </summary>
        public int Cost { get; set; }
    }
}
