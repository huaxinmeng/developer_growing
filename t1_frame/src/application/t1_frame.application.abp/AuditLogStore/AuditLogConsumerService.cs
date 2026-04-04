using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using t1_frame.entityframeworkcore.abp;
using Volo.Abp.Auditing;
using Volo.Abp.AuditLogging;
using Volo.Abp.AuditLogging.MongoDB;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories.MongoDB;
using System.Text.Json.Serialization;
using Volo.Abp.Kafka;
using static Confluent.Kafka.ConfigPropertyNames;
using YamlDotNet.Serialization;

namespace t1_frame.application.abp
{
    public class AuditLogConsumerService : BackgroundService
    {
        private readonly IConsumer<Ignore, string> _consumer;
        private readonly IMongoCollection<AuditLog> _collection;
        private readonly Channel<AuditLog> _channel;
        private readonly CancellationTokenSource _cts;
        private Task _processorTask;
        private Task _executingTask;
        private readonly int _batchSize;
        private readonly int _maxWaitMs;
        // private readonly IKafkaMessageConsumer _consumer1;

        public AuditLogConsumerService(
            IConfiguration config,
            IMongoClient mongoClient)
        {
            // Kafka 消费者配置
            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = config["Kafka:Connections:Default:BootstrapServers"] ?? "localhost:9092",
                GroupId = config["Kafka:EventBus:GroupId"] ?? "audit-log-consumers",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false,           // 手动提交，确保数据写入后才确认
                MaxPollIntervalMs = 300000,         // 5分钟最大轮询间隔（批量处理需要）
                SessionTimeoutMs = 45000,           // 会话超时
                HeartbeatIntervalMs = 3000,         // 心跳间隔
                FetchMinBytes = 1024 * 1024,        // 至少 1MB 数据才获取（提高吞吐量）
                FetchMaxBytes = 50 * 1024 * 1024,   // 最大 50MB
                MaxPartitionFetchBytes = 50 * 1024 * 1024
            };

            _consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build();
            _consumer.Subscribe(config["Kafka:EventBus:TopicName"] ?? "audit-logs");

            // _consumer1 = consumer1;
            // MongoDB 集合
            var database = mongoClient.GetDatabase("t1_frame");
            _collection = database.GetCollection<AuditLog>("AbpAuditLogs");
            // 创建索引（如果尚不存在）
             // CreateIndexes();

            // 配置参数
            _batchSize = config.GetValue<int>("AuditLog:BatchSize", 1000);
            _maxWaitMs = config.GetValue<int>("AuditLog:MaxWaitMs", 500);

            //// 创建有界 Channel（背压控制）
            //_channel = Channel.CreateBounded<AuditLog>(new BoundedChannelOptions(10000)
            //{
            //    FullMode = BoundedChannelFullMode.DropOldest,  // 满了丢弃最老的（审计日志可容忍丢失）
            //    SingleReader = true,
            //    SingleWriter = true
            //});

            // 创建无界通道（背压处理）
            _channel = Channel.CreateUnbounded<AuditLog>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

            _cts = new CancellationTokenSource();
            _processorTask = Task.Factory.StartNew(async () => await ProcessBatchAsync(_cts.Token), TaskCreationOptions.LongRunning).Unwrap(); ;
        }

        //private void CreateIndexes()
        //{
        //    // 创建常用查询索引
        //    var indexKeys = Builders<AuditLog>.IndexKeys
        //        .Descending(x => x.ExecutionTime)
        //        .Ascending(x => x.UserId)
        //        .Ascending(x => x.TenantId);

        //    _collection.Indexes.CreateOne(new CreateIndexModel<AuditLog>(indexKeys));
        //}

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver
                {
                    // 允许反序列化到非公共 setter
                    DefaultMembersSearchFlags = System.Reflection.BindingFlags.Instance |
                                                System.Reflection.BindingFlags.Public |
                                                System.Reflection.BindingFlags.NonPublic
                },
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                NullValueHandling = NullValueHandling.Ignore,
            };

            await Task.CompletedTask;
            //_consumer1.OnMessageReceived(async (message) =>
            //{
            //    try
            //    {
            //        var content = Encoding.UTF8.GetString(message.Value);
            //        // Console.WriteLine($"Topic: {message.Topic}, Key: {message.Key}, Value: {content}");
            //        var auditLog = JsonConvert.DeserializeObject<AuditLogInfo>(content, settings);
            //        if (auditLog == null) return;
            //        await _collection.InsertManyAsync([auditLog], new InsertManyOptions
            //        {
            //            IsOrdered = false,
            //            BypassDocumentValidation = true
            //        }, stoppingToken);

            //        // 业务处理
            //        // await ProcessMessageAsync(content);

            //        // 注意：此接口没有返回值，异常需要自己处理
            //        // 如需手动提交，需要在配置中 EnableAutoCommit = false，然后调用 Commit()
            //    }
            //    catch (Exception ex)
            //    {
            //        // 记录异常，这里不会自动重试或进入死信队列
            //        Console.WriteLine($"Error: {ex.Message}");
            //        throw; // 抛出后消费会中断，需要自行实现重试逻辑
            //    }
            //});

            //_executingTask = Task.Factory.StartNew(async () =>
            //{
            //    while (!stoppingToken.IsCancellationRequested)
            //    {
            //        try
            //        {
            //            var result = _consumer.Consume(stoppingToken);

            //            // Console.WriteLine($"Message: {result.Message.Value}");

            //            var content = result.Message.Value;//Encoding.UTF8.GetString(result.Message.Value);
            //                                               // Console.WriteLine($"Topic: {message.Topic}, Key: {message.Key}, Value: {content}");
            //            var auditLog = JsonConvert.DeserializeObject<AuditLog>(content, settings);
            //            if (auditLog == null) continue;
            //            await _collection.InsertManyAsync([auditLog], new InsertManyOptions
            //            {
            //                IsOrdered = false,
            //                BypassDocumentValidation = true
            //            }, stoppingToken);

            //            // 业务处理
            //            // await ProcessAsync(result.Message.Value);

            //            // 手动提交偏移量
            //            _consumer.Commit(result);
            //        }
            //        catch (ConsumeException ex)
            //        {
            //            Console.WriteLine($"Error: {ex.Error.Reason}");
            //        }
            //    }
            //}, TaskCreationOptions.LongRunning).Unwrap();

            _executingTask = Task.Factory.StartNew(async () =>
            {
                var messages = new List<ConsumeResult<Ignore, string>>();
                try
                {
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        try
                        {
                            // 批量消费 Kafka 消息
                            // var messages = new List<ConsumeResult<Ignore, string>>();
                            messages.Clear();
                            // 等待第一批消息
                            var firstMessage = _consumer.Consume(TimeSpan.FromMilliseconds(_maxWaitMs));
                            if (firstMessage == null) continue;

                            messages.Add(firstMessage);

                            // 快速读取后续消息，直到批次满或超时
                            var deadline = DateTime.UtcNow.AddMilliseconds(_maxWaitMs);
                            while (messages.Count < _batchSize && DateTime.UtcNow < deadline)
                            {
                                var remainingTime = deadline - DateTime.UtcNow;
                                if (remainingTime <= TimeSpan.Zero) break;

                                var msg = _consumer.Consume(remainingTime);
                                if (msg == null) break;
                                messages.Add(msg);
                            }

                            // 解析并写入 Channel
                            foreach (var msg in messages)
                            {
                                try
                                {
                                    //var auditLog = JsonSerializer.Deserialize<AuditLog>(msg.Message.Value, new JsonSerializerOptions
                                    //{
                                    //    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                                    //});

                                    var auditLog = JsonConvert.DeserializeObject<AuditLog>(msg.Message.Value, settings);

                                    if (auditLog != null)
                                    {
                                        await _channel.Writer.WriteAsync(auditLog, stoppingToken);
                                    }
                                }
                                catch (JsonException ex)
                                {
                                    Console.WriteLine($"消息解析失败: {ex.Message}");
                                    // 继续处理其他消息，不提交此消息的 offset
                                }
                            }

                            // 等待批量写入完成
                            // await WaitForBatchCompleteAsync(messages.Count, stoppingToken);

                            // 提交 offset（确认消息已处理）
                            _consumer.Commit(messages.Last());
                        }
                        catch (ConsumeException ex)
                        {
                            Console.WriteLine($"Kafka 消费错误: {ex.Error.Reason}");
                            await Task.Delay(1000, stoppingToken);
                        }
                    }
                }
                finally
                {
                    _channel.Writer.Complete();
                    await _processorTask;
                }

            }, TaskCreationOptions.LongRunning).Unwrap();
        }

        private async Task WaitForBatchCompleteAsync(int count, CancellationToken ct)
        {
            // 简单等待策略：假设处理速度足够快，或者使用信号量
            var timeout = TimeSpan.FromSeconds(30);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeout);

            try
            {
                // 等待 Channel 中的数据被消费
                while (_channel.Reader.Count > 0 && !cts.Token.IsCancellationRequested)
                {
                    await Task.Delay(10, cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"等待批次完成超时，Channel 剩余: {_channel.Reader.Count}");
            }
        }

        private async Task ProcessBatchAsync(CancellationToken ct)
        {
            var batch = new List<AuditLog>(_batchSize);

            //await foreach (var item in _channel.Reader.ReadAllAsync(ct))
            //{
            //    batch.Add(item);

            //    // 批次满或 Channel 空了，立即写入
            //    if (batch.Count >= _batchSize || _channel.Reader.Count == 0)
            //    {
            //        await FlushBatchAsync(batch, ct);
            //        batch.Clear();
            //    }
            //}

            //// 处理剩余数据
            //if (batch.Count > 0)
            //{
            //    await FlushBatchAsync(batch, ct);
            //}


            while (!ct.IsCancellationRequested)
            { 
                if(!_channel.Reader.TryRead(out var item) && batch.Count > 0)
                {
                    await FlushBatchAsync(batch, ct);
                    batch.Clear();

                    continue;
                }

                if (item == null) continue;
                batch.Add(item);
                if (batch.Count >= _batchSize)
                {
                    await FlushBatchAsync(batch, ct);
                    batch.Clear();
                }
            }
        }

        private async Task FlushBatchAsync(List<AuditLog> batch, CancellationToken ct)
        {
            if (batch.Count == 0) return;

            var retryCount = 0;
            const int maxRetries = 3;

            while (retryCount < maxRetries)
            {
                try
                {
                    // 使用无序批量写入（性能最优）
                    await _collection.InsertManyAsync(batch, new InsertManyOptions
                    {
                        IsOrdered = false,
                        BypassDocumentValidation = true
                    },ct);

                    Console.WriteLine($"成功写入 {batch.Count} 条审计日志");
                    return;
                }
                catch (MongoBulkWriteException ex) when (retryCount < maxRetries - 1)
                {
                    retryCount++;
                    Console.WriteLine($"MongoDB 写入失败，重试 {retryCount}/{maxRetries}: {ex.Message}");
                    await Task.Delay(TimeSpan.FromMilliseconds(100 * retryCount), ct);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"MongoDB 写入失败，放弃重试: {ex.Message}");
                    // 记录到备用存储或死信队列
                    throw;
                }
            }
        }

        public override void Dispose()
        {
            _cts.Cancel();
            _consumer?.Close();
            _consumer?.Dispose();
            base.Dispose();
        }
    }
}
