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
    // [EventName("Goods.StockRace")]
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

    [EventName("Goods.StockRace")]
    public class GoodsRaceActEto : GoodsRaceEto
    {
        public GoodsRaceActEto()
        {

        }

        public GoodsRaceActEto(GoodsRaceEto eto, string requestId)
        {
            user_code = eto.user_code;
            goods_name = eto.goods_name;
            goods_stock = eto.goods_stock;

            occur_time = DateTime.UtcNow;

            request_id = requestId;
        }

        public string request_id {  get; set; }

        public DateTime occur_time { get; set; }
    }
}
