using System.Text.Json;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace t1_frame.application.abp.Seckill
{
    /// <summary>
    /// Kafka 批量消费者：
    /// 手动提交 offset，一个批次一个 MySQL 事务；落库成功后提交 offset。
    /// 至少一次投递 + order_no 唯一约束，保证重复消息不会重复扣库存/建单。
    /// </summary>
    public sealed class SeckillKafkaConsumer : BackgroundService
    {
        private const int BatchSize = 500;
        private const int WriteRetryCount = 3;

        private readonly SeckillKafkaOptions _options;
        private readonly SeckillBatchWriter _writer;
        private readonly SeckillQueue _queue;
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<SeckillKafkaConsumer> _logger;

        public SeckillKafkaConsumer(
            SeckillKafkaOptions options,
            SeckillBatchWriter writer,
            SeckillQueue queue,
            IConnectionMultiplexer redis,
            ILogger<SeckillKafkaConsumer> logger)
        {
            _options = options;
            _writer = writer;
            _queue = queue;
            _redis = redis;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await EnsureTopicAsync(stoppingToken);

            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = _options.BootstrapServers,
                GroupId = _options.GroupId,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false,
                SessionTimeoutMs = 10_000,
                MaxPollIntervalMs = 300_000,
                FetchMinBytes = 1
            };

            using var consumer = new ConsumerBuilder<string, string>(consumerConfig)
                .SetErrorHandler((_, error) => _logger.LogWarning(
                    "Kafka consumer error: {Code} {Reason}",
                    error.Code,
                    error.Reason))
                .Build();

            consumer.Subscribe(_options.Topic);

            _logger.LogInformation(
                "Seckill Kafka consumer started: topic={Topic}, group={Group}, brokers={Brokers}",
                _options.Topic,
                _options.GroupId,
                _options.BootstrapServers);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var batch = new List<SeckillOrderMessage>(BatchSize);
                    var consumed = new List<ConsumeResult<string, string>>(BatchSize);

                    while (batch.Count < BatchSize)
                    {
                        ConsumeResult<string, string>? result;

                        try
                        {
                            result = consumer.Consume(TimeSpan.FromMilliseconds(200));
                        }
                        catch (OperationCanceledException)
                        {
                            throw;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Kafka 消费异常，200ms 后继续");
                            await Task.Delay(200, stoppingToken);
                            result = null;
                        }

                        if (result == null)
                        {
                            break;
                        }

                        if (result.IsPartitionEOF)
                        {
                            continue;
                        }

                        try
                        {
                            var message = JsonSerializer.Deserialize<SeckillOrderMessage>(
                                result.Message.Value);

                            if (message == null)
                            {
                                _logger.LogWarning(
                                    "Kafka 秒杀消息为空，跳过 offset={Offset}",
                                    result.Offset);
                                consumer.Commit(ToOffsets(new[] { result }));
                                continue;
                            }

                            batch.Add(message);
                            consumed.Add(result);
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogWarning(
                                ex,
                                "Kafka 秒杀消息无法反序列化，跳过 offset={Offset}",
                                result.Offset);
                            consumer.Commit(ToOffsets(new[] { result }));
                        }
                    }

                    if (batch.Count == 0)
                    {
                        continue;
                    }

                    try
                    {
                        await ProcessBatchAsync(consumer, batch, consumed, stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        // 不提交 offset，下一轮 Kafka 会重新投递；落库侧是幂等的。
                        _logger.LogError(ex, "秒杀批次处理异常，未提交 offset，等待重新投递");
                        await Task.Delay(1000, stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 正常停机
            }
            finally
            {
                try
                {
                    consumer.Close();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Kafka consumer 关闭失败");
                }
            }
        }

        private async Task ProcessBatchAsync(
            IConsumer<string, string> consumer,
            List<SeckillOrderMessage> batch,
            List<ConsumeResult<string, string>> consumed,
            CancellationToken stoppingToken)
        {
            var success = false;
            Exception? lastError = null;

            for (var attempt = 1; attempt <= WriteRetryCount; attempt++)
            {
                try
                {
                    await _writer.WriteAsync(batch, stoppingToken);
                    success = true;
                    break;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    _logger.LogWarning(
                        ex,
                        "秒杀批量落库失败，第 {Attempt}/{Total} 次重试，batch={Count}",
                        attempt,
                        WriteRetryCount,
                        batch.Count);

                    if (attempt < WriteRetryCount)
                    {
                        await Task.Delay(200 * attempt, stoppingToken);
                    }
                }
            }

            if (success)
            {
                await SetStatusesAsync(batch, "created");
                _queue.MarkCreated(batch.Count);
                consumer.Commit(ToOffsets(consumed));
                return;
            }

            // 落库连续失败：标记失败并提交 offset，避免毒消息无限阻塞分区。
            // 生产环境应同时写入死信队列，并由对账任务补偿 Redis 库存。
            await SetStatusesAsync(batch, "failed");
            _queue.MarkFailed(batch.Count);
            consumer.Commit(ToOffsets(consumed));

            _logger.LogError(
                lastError,
                "秒杀批次最终落库失败，已标记 failed 并提交 offset，batch={Count}；需要死信/对账补偿",
                batch.Count);
        }

        private static IEnumerable<TopicPartitionOffset> ToOffsets(
            IEnumerable<ConsumeResult<string, string>> results)
        {
            // Kafka 提交的是“下一条要消费的 offset”
            return results.Select(result => new TopicPartitionOffset(
                result.TopicPartition,
                result.Offset + 1));
        }

        private async Task SetStatusesAsync(List<SeckillOrderMessage> batch, string status)
        {
            var redis = _redis.GetDatabase();
            var redisBatch = redis.CreateBatch();

            var tasks = batch
                .Select(message => redisBatch.StringSetAsync(
                    SeckillService.StatusKey(message.RunId, message.RequestId),
                    status,
                    TimeSpan.FromHours(1)))
                .ToList();

            redisBatch.Execute();
            await Task.WhenAll(tasks);
        }

        private async Task EnsureTopicAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var admin = new AdminClientBuilder(new AdminClientConfig
                    {
                        BootstrapServers = _options.BootstrapServers
                    }).Build();

                    await admin.CreateTopicsAsync(new[]
                    {
                        new TopicSpecification
                        {
                            Name = _options.Topic,
                            NumPartitions = _options.Partitions,
                            ReplicationFactor = _options.ReplicationFactor
                        }
                    });

                    return;
                }
                catch (CreateTopicsException ex) when (
                    ex.Results.All(result => result.Error.Code == ErrorCode.TopicAlreadyExists))
                {
                    return;
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "创建 Kafka topic {Topic} 失败，2 秒后重试（brokers={Brokers}）",
                        _options.Topic,
                        _options.BootstrapServers);

                    await Task.Delay(2000, stoppingToken);
                }
            }
        }
    }
}
