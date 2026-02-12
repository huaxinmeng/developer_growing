using System.Data;

namespace t1_frame.common
{
    [AttributeUsage(AttributeTargets.Property)]
    public class FieldAttribute : Attribute
    {
        public FieldAttribute()
        {
        }

        public FieldAttribute(string name)
        {
            ColumnName = name;
        }

        /// <summary>
        /// 是否主键
        /// </summary>
        public bool IsPrimaryKey { set; get; }

        /// <summary>
        /// 主键是否自动增长
        /// </summary>
        public bool IsIdentity { set; get; }

        /// <summary>
        /// 是否非空字段
        /// </summary>
        public bool IsNotNull { set; get; }

        /// <summary>
        /// 列名
        /// </summary>
        public string ColumnName { set; get; }

        /// <summary>
        /// 数据类型
        /// </summary>
        public DbType DataType { get; set; }
    }
}