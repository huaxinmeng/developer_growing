using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Volo.Abp.Domain.Entities;

namespace t1_frame.entityframeworkcore.abp
{
    /// <summary>
    /// 商品目录信息（价格、描述、图片、是否上架），与 t1_goods_stock 一对一。
    /// </summary>
    [Table("t1_inventory_product_detail")]
    public class T1InventoryProductDetail : Entity<long>
    {
        public T1InventoryProductDetail()
        {
        }

        public T1InventoryProductDetail(long productId)
            : base(productId)
        {
        }

        public decimal price { get; set; }

        [MaxLength(500)]
        public string? description { get; set; }

        [MaxLength(500)]
        public string? image_url { get; set; }

        [MaxLength(50)]
        public string? category { get; set; }

        public bool is_on_sale { get; set; }

        public DateTime created_at { get; set; }
    }
}
