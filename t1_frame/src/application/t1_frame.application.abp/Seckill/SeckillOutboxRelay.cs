using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace t1_frame.application.abp.Seckill
{
    /// <summary>
    /// Redis List outbox 投递器：
    /// outbox --RPOPLPUSH--> processing --Kafka(acks=all)--> 删除 processing。
    /// 若进程在投递后、删除前崩溃，processing 中的消息会在下次启动时回到 outbox 重投；
    /// 消费者按 order_no 幂等，因此允许重复。
    /// </summary>
    public sealed class SeckillOutboxRelay : BackgroundService
    {
        private const int ReadCount = 200;

        private readonly IConnectionMultiplexer _redis;
        private readonly SeckillKafkaOptions _options;
        private readonly ILogger<SeckillOutboxRelay> _logger;
        private readonly IProducer<string, string> _producer;

        public SeckillOutboxRelay(
            IConnectionMultiplexer redis,
            SeckillKafkaOptions options,
            ILogger<SeckillOutboxRelay> logger)
        {
            _redis = redis;
            _options = options;
            _logger = logger;

            var config = new ProducerConfig
            {
                BootstrapServers = options.BootstrapServers,
                Acks = Acks.All,
                EnableIdempotence = true,
                LingerMs = 5,
                MessageTimeoutMs = 10_000,
                SocketTimeoutMs = 10_000
            };

            _producer = new ProducerBuilder<string, string>(config).Build();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var redis = _redis.GetDatabase();

            await RecoverProcessingAsync(redis, stoppingToken);

            _logger.LogInformation(
                "Seckill outbox relay started: outbox={Outbox}, processing={Processing}, topic={Topic}, brokers={Brokers}",
                SeckillService.OutboxKey,
                SeckillService.ProcessingKey,
                _options.Topic,
                _options.BootstrapServers);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var payloads = await PopBatchAsync(redis);

                    if (payloads.Count == 0)
                    {
                        await Task.Delay(50, stoppingToken);
                        continue;
                    }

                    try
                    {
                        // Kafka producer 线程安全；并发投递让 broker 端自动攒批。
                        await Task.WhenAll(payloads.Select(payload => _producer.ProduceAsync(
                            _options.Topic,
                            new Message<string, string> { Value = payload },
                            stoppingToken)));
                    }
                    catch
                    {
                        // 投递失败：把本批消息放回 outbox，避免停留在 processing 丢失。
                        await ReturnToOutboxAsync(redis, payloads);
                        throw;
                    }

                    await RemoveFromProcessingAsync(redis, payloads);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "秒杀 outbox 投递 Kafka 失败，稍后重试；消息会保留在 Redis List 中");

                    // 若失败发生在“放回 outbox”过程中，processing 里可能残留消息。
                    // 单实例 relay 没有并发批处理，这里可以安全地全部搬回 outbox。
                    try
                    {
                        await RecoverProcessingAsync(redis, stoppingToken);
                    }
                    catch (Exception recoveryError)
                    {
                        _logger.LogError(recoveryError, "恢复秒杀 processing 列表失败");
                    }

                    await Task.Delay(1000, stoppingToken);
                }
            }
        }

        public override void Dispose()
        {
            _producer.Dispose();
            base.Dispose();
        }

        private async Task RecoverProcessingAsync(IDatabase redis, CancellationToken stoppingToken)
        {
            var recovered = 0;

            while (!stoppingToken.IsCancellationRequested)
            {
                var value = await redis.ListRightPopLeftPushAsync(
                    SeckillService.ProcessingKey,
                    SeckillService.OutboxKey);

                if (value.IsNull)
                {
                    break;
                }

                recovered++;
            }

            if (recovered > 0)
            {
                _logger.LogWarning(
                    "秒杀 outbox 启动恢复：{Count} 条 processing 消息已放回 outbox 重投",
                    recovered);
            }
        }

        private static async Task<List<string>> PopBatchAsync(IDatabase redis)
        {
            // 用 pipeline 一次取一批，减少 Redis 往返。
            var batch = redis.CreateBatch();
            var tasks = Enumerable.Range(0, ReadCount)
                .Select(_ => batch.ListRightPopLeftPushAsync(
                    SeckillService.OutboxKey,
                    SeckillService.ProcessingKey))
                .ToArray();

            batch.Execute();
            var values = await Task.WhenAll(tasks);

            return values
                .Where(value => !value.IsNull)
                .Select(value => value.ToString())
                .ToList();
        }

        private static async Task RemoveFromProcessingAsync(IDatabase redis, List<string> payloads)
        {
            var batch = redis.CreateBatch();
            var tasks = payloads
                .Select(payload => (Task)batch.ListRemoveAsync(
                    SeckillService.ProcessingKey,
                    payload,
                    1))
                .ToList();

            batch.Execute();
            await Task.WhenAll(tasks);
        }

        private static async Task ReturnToOutboxAsync(IDatabase redis, List<string> payloads)
        {
            var batch = redis.CreateBatch();
            var tasks = new List<Task>(payloads.Count * 2);

            foreach (var payload in payloads)
            {
                tasks.Add(batch.ListRemoveAsync(SeckillService.ProcessingKey, payload, 1));
                tasks.Add(batch.ListLeftPushAsync(SeckillService.OutboxKey, payload));
            }

            batch.Execute();
            await Task.WhenAll(tasks);
        }
    }
}
