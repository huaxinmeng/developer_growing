//------------------------------------------------------------------------------
// 警告:
//     该代码由T4工具根据模板自动生成,直接在该代码上进行任何修改都将在重新生成后丢失!
//     如有需要应使用partial class或是继承该类进行自定义扩展。
//------------------------------------------------------------------------------
using t1_frame.common;

namespace t1_frame.dbfirst
{
    /// <summary>
    /// 实体-T1_api_base
    /// </summary>
    [Table("t1_api_base")]
    public partial class T1ApiBaseEntity : BaseEntity
    {
        /// <summary>
        ///
        /// </summary>

        public string base_code { get; set; }
        /// <summary>
        ///
        /// </summary>

        public string create_code { get; set; }
        /// <summary>
        ///
        /// </summary>

        public DateTime create_on { get; set; }

        /// <summary>
        ///
        /// </summary>
        [Field(IsPrimaryKey = true, IsIdentity = true)]
        public long id { get; set; }

        /// <summary>
        ///
        /// </summary>

        public string remarks { get; set; }
        /// <summary>
        ///
        /// </summary>

        public string sys_code { get; set; }
        /// <summary>
        ///
        /// </summary>

        public string sys_name { get; set; }
        /// <summary>
        ///
        /// </summary>

        public string sys_url { get; set; }
        /// <summary>
        ///
        /// </summary>

        public string update_code { get; set; }
        /// <summary>
        ///
        /// </summary>

        public DateTime update_on { get; set; }
    }
}