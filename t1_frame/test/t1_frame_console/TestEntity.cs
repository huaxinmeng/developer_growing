using System.Collections;

namespace t1_frame_console
{
    public class TestEntity : ITest, IService
    {
        /// <summary>
        /// 配板单号
        /// </summary>
        public string board_number { get; set; }

        /// <summary>
        /// 箱单文件地址
        /// </summary>
        public string box_url { get; set; }

        /// <summary>
        /// 箱单本地文件地址
        /// </summary>
        public string box_urlname { get; set; }

        /// <summary>
        /// 袋子数
        /// </summary>
        public int bag_count { get; set; }

        /// <summary>
        /// 生效时间
        /// </summary>
        public  DateTime valid_date { get; set; }

        /// <summary>
        /// 是否有效
        /// </summary>
        public bool is_valid {  get; set; }

        public bool is_first {  get; set; }

        /// <summary>
        /// 箱单文件信息
        /// </summary>
       // public FileTestEntity box_info { get; set; }

        /// <summary>
        /// 附件信息
        /// </summary>
       // public List<FileTestEntity> attachments { get; set; }

        public int GetStr(string node)
        {
            return 10;
        }

        public int GetStr0()
        {
            return GetStr("2356");
            //return ((ITest)this).GetStr("0");
        }

        int ITest.GetStr(string node)
        {
            return 5;
        }

        int IService.GetStr(string node)
        {
            return 6;
        }
    }  

    public class TestEntityComparer : IComparer<TestEntity>
    {
        int IComparer<TestEntity>.Compare(TestEntity? x, TestEntity? y)
        {
            //int value = x.bag_count.CompareTo(y.bag_count);
            //if(value != 0)
            //{
            //    return value;
            //}
            //return ((new CaseInsensitiveComparer()).Compare(y?.board_number, x?.board_number));
            return ((new CaseInsensitiveComparer()).Compare(x?.board_number, y?.board_number));
        }
    }
   

    public class FileTestEntityComparer : IComparer<FileTestEntity>
    {
        int IComparer<FileTestEntity>.Compare(FileTestEntity? x, FileTestEntity? y)
        {
            //return ((new CaseInsensitiveComparer()).Compare(y?.box_urlname, x?.box_urlname));
            return ((new CaseInsensitiveComparer()).Compare(x?.box_urlname, y?.box_urlname));
        }
    }

    public class FileTestEntity
    {
        /// <summary>
        /// 箱单文件地址
        /// </summary>
        public string box_url { get; set; }

        /// <summary>
        /// 箱单本地文件地址
        /// </summary>
        public string box_urlname { get; set; }
    }
}