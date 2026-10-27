using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace t1_frame.entityframeworkcore.abp
{
    /// <summary>
    /// MongoDB 中的扣减审计日志。
    /// 用 biz_no 作为 _id，消息重复消费时天然幂等。
    /// </summary>
    [BsonIgnoreExtraElements]
    public class InventoryDeductionLog
    {
        [BsonId]
        public string biz_no { get; set; } = string.Empty;

        public long product_id { get; set; }

        public string product_name { get; set; } = string.Empty;

        public int quantity { get; set; }

        public int remaining_stock { get; set; }

        public string source { get; set; } = string.Empty;

        public DateTime created_at { get; set; }
    }
}
