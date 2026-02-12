using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace t1_frame.condition
{
    public class TestCondition
    {
        /// <summary>
        /// 是否定标（是：Y，否：N）
        /// </summary>
        [Required]
        public string IsTarget { get; set; }
    }
}
