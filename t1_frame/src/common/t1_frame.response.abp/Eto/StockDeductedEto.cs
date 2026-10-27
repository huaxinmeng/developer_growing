using Volo.Abp.EventBus;

namespace t1_frame.response.abp
{
    /// <summary>
    /// 库存扣减成功后通过分布式事件总线（RabbitMQ / Kafka）发布。
    /// </summary>
    [EventName("Inventory.StockDeducted")]
    public class StockDeductedEto
    {
        public string biz_no { get; set; } = string.Empty;

        public long product_id { get; set; }

        public string product_name { get; set; } = string.Empty;

        public int quantity { get; set; }

        public int remaining_stock { get; set; }

        public string source { get; set; } = string.Empty;

        public DateTime occurred_at { get; set; }
    }
}
