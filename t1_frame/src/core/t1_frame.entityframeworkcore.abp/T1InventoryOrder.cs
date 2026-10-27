using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Volo.Abp.Domain.Entities;

namespace t1_frame.entityframeworkcore.abp
{
    /// <summary>
    /// 库存扣减演示订单（MySQL）。
    /// </summary>
    [Table("t1_inventory_order")]
    public class T1InventoryOrder : Entity<long>
    {
        [Required]
        [MaxLength(64)]
        public string order_no { get; set; } = string.Empty;

        /// <summary>
        /// 销售单号；商店订单的多行共享同一个销售单号，库存演示订单为空。
        /// </summary>
        [MaxLength(64)]
        public string? sale_no { get; set; }

        public long product_id { get; set; }

        public int quantity { get; set; }

        /// <summary>
        /// 单价；库存演示订单为 0，商店销售订单为商品价格。
        /// </summary>
        public decimal unit_price { get; set; }

        /// <summary>
        /// 买家名称（演示用，可为空）。
        /// </summary>
        [MaxLength(100)]
        public string? customer_name { get; set; }

        /// <summary>
        /// 1 = 待支付，2 = 已支付，5 = 已关闭
        /// </summary>
        public int status { get; set; }

        public DateTime created_at { get; set; }
    }
}
