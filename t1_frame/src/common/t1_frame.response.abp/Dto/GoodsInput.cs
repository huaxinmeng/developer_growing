using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace t1_frame.response.abp
{
    public class GoodsInput
    {
        public string goods_name { get; set; }

        /// <summary>
        /// 商品库存
        /// </summary>
        public int goods_stock { get; set; }
    }
}
