using Abp.Domain.Entities;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace t1_frame.domain
{
    [Table("t1_api_address")]
    public class T1ApiAddress : Entity<long>
    {
        /// <summary>
        /// API地址
        /// </summary>
        [NotNull]
        public string api_address { get; set; }

        /// <summary>
        /// API编码
        /// </summary>
        [NotNull]
        public string api_code { get; set; }

        /// <summary>
        /// API地址名称
        /// </summary>
        [NotNull]
        public string api_name { get; set; }

        /// <summary>
        /// 关联系统地址编码
        /// </summary>
        [NotNull]
        public string base_code { get; set; }

        /// <summary>
        /// 创建人编码
        /// </summary>
        [NotNull]
        public string create_code { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>

        public DateTime create_on { get; set; }

        ///// <summary>
        ///// 主键
        ///// </summary>
        //public long id { get; set; }

        /// <summary>
        /// 更新人编码
        /// </summary>
        [NotNull]
        public string update_code { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>

        public DateTime update_on { get; set; }
    }
}