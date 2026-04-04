using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson.Serialization.IdGenerators;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Volo.Abp.Auditing;
using Volo.Abp.AuditLogging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Guids;

namespace t1_frame.application.abp
{
    public class KafkaAuditingStore : IAuditingStore, ISingletonDependency, IDisposable
    {
        private readonly IProducer<Null, string> _producer;
        private readonly IGuidGenerator _guidGenerator;
        private readonly string _topic;
        private bool _disposed;

        public KafkaAuditingStore(IConfiguration config, IGuidGenerator guidGenerator)
        {
            var producerConfig = new ProducerConfig
            {
                BootstrapServers = config["Kafka:Connections:Default:BootstrapServers"] ?? "localhost:9192",
                // BatchSize = 1000000,  // 1MB批次
                LingerMs = 100,       // 100ms聚合
                CompressionType = CompressionType.Lz4
            };
            _producer = new ProducerBuilder<Null, string>(producerConfig).Build();
            _topic = config["Kafka:EventBus:TopicName"] ?? "audit-logs";
            _guidGenerator = guidGenerator;
        }

        public async Task SaveAsync(AuditLogInfo auditInfo)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(ConvertToAuditLog(auditInfo));
            await _producer.ProduceAsync(_topic, new Message<Null, string> { Value = json });
            // 立即返回，实际写入由Kafka消费者异步处理
        }

        private AuditLog ConvertToAuditLog(AuditLogInfo info)
        {
            var id = _guidGenerator.Create();
            var actions = info.Actions?
            .Select(actionInfo => new AuditLogAction(
                _guidGenerator.Create(),
                id,
                actionInfo,
                info.TenantId))
            .ToList() ?? new List<AuditLogAction>();

            var entityChanges = info.EntityChanges?
                .Select(entityChangeInfo => new EntityChange(
                    _guidGenerator,
                    id,
                    entityChangeInfo,
                    info.TenantId))
                .ToList() ?? new List<EntityChange>();
            return new AuditLog(id,
                info.ApplicationName,
                info.TenantId,
                info.TenantName,
                info.UserId,
                info.UserName,
                info.ExecutionTime,
                info.ExecutionDuration,
                info.ClientIpAddress,
                info.ClientName,
                info.ClientId,
                info.CorrelationId,
                info.BrowserInfo,
                info.HttpMethod,
                info.Url,
                info.HttpStatusCode,
                info.ImpersonatorUserId,
                info.ImpersonatorUserName,
                info.ImpersonatorTenantId,
                info.ImpersonatorTenantName,
                info.ExtraProperties,
                entityChanges,
                actions,
                JsonConvert.SerializeObject(info.Exceptions),
                JsonConvert.SerializeObject(info.Comments));
        }

        public void Dispose()
        {
            if (_disposed) return;

            // 关键：Flush确保消息发送完成
            _producer?.Flush(TimeSpan.FromSeconds(10));
            _producer?.Dispose();
            _disposed = true;
        }
    }
}
