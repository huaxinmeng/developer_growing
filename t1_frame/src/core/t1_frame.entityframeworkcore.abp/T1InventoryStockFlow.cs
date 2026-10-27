using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Volo.Abp.Domain.Entities;

namespace t1_frame.entityframeworkcore.abp
{
    /// <summary>
    /// 库存扣减流水（MySQL），biz_no 唯一索引用于幂等。
    /// </summary>
    [Table("t1_inventory_stock_flow")]
    public class T1InventoryStockFlow : Entity<long>
    {
        [Required]
        [MaxLength(64)]
        public string biz_no { get; set; } = string.Empty;

        public long product_id { get; set; }

        [MaxLength(100)]
        public string product_name { get; set; } = string.Empty;

        public int quantity { get; set; }

        /// <summary>
        /// 扣减来源：atomic / optimistic / redis / idempotent
        /// </summary>
        [MaxLength(32)]
        public string source { get; set; } = string.Empty;

        public int remaining_stock { get; set; }

        public DateTime created_at { get; set; }
    }
}
