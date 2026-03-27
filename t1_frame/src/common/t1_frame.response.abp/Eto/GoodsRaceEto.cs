using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Domain.Entities;
using Volo.Abp.EventBus;

namespace t1_frame.response.abp
{
    [EventName("Goods.StockRace")]
    public class GoodsRaceEto
    {
        [Required]
        public string user_code { get; set; }

        public string goods_name { get; set; }

        /// <summary>
        /// 商品库存
        /// </summary>
        public int goods_stock { get; set; }
    }
}
