using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using t1_frame.entityframeworkcore.abp;
using t1_frame.response.abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace t1_frame.application.abp
{
    /// <summary>
    /// 消费库存扣减事件（RabbitMQ / Kafka），把审计日志写入 MongoDB。
    /// </summary>
    public class StockDeductedHandler : IDistributedEventHandler<StockDeductedEto>, ITransientDependency
    {
        private const string CollectionName = "t1_inventory_deduction_log";

        private readonly IMongoClient _mongoClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<StockDeductedHandler> _logger;

        public StockDeductedHandler(
            IMongoClient mongoClient,
            IConfiguration configuration,
            ILogger<StockDeductedHandler> logger)
        {
            _mongoClient = mongoClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task HandleEventAsync(StockDeductedEto eventData)
        {
            var connectionString = _configuration.GetConnectionString("mongodb")
                                   ?? "mongodb://localhost:27017/t1_frame";
            var url = new MongoUrl(connectionString);
            var database = _mongoClient.GetDatabase(url.DatabaseName ?? "t1_frame");
            var collection = database.GetCollection<InventoryDeductionLog>(CollectionName);

            var log = new InventoryDeductionLog
            {
                biz_no = eventData.biz_no,
                product_id = eventData.product_id,
                product_name = eventData.product_name,
                quantity = eventData.quantity,
                remaining_stock = eventData.remaining_stock,
                source = eventData.source,
                created_at = eventData.occurred_at
            };

            // 用 biz_no 作为 _id，消息重复投递时覆盖而不是新增
            await collection.ReplaceOneAsync(
                x => x.biz_no == eventData.biz_no,
                log,
                new ReplaceOptions { IsUpsert = true });

            _logger.LogDebug(
                "库存扣减审计日志已写入 MongoDB，bizNo={BizNo}, source={Source}",
                eventData.biz_no,
                eventData.source);
        }
    }
}
