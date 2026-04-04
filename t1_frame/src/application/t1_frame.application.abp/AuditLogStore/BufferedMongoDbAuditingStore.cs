using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using Volo.Abp.Auditing;
using Volo.Abp.AuditLogging;
using Volo.Abp.DependencyInjection;

namespace t1_frame.application.abp
{
    public class BufferedMongoDbAuditingStore : IAuditingStore, ISingletonDependency, IDisposable
    {
        private readonly IMongoCollection<AuditLog> _collection;
        private readonly Channel<AuditLogInfo> _channel;
        private readonly CancellationTokenSource _cts;
        private readonly Task _consumerTask;

        // 批量写入大小
        private const int BatchSize = 1000;
        // 最大等待时间(ms)
        private const int MaxWaitMs = 500;

        public BufferedMongoDbAuditingStore(IMongoClient mongoClient)
        {
            var database = mongoClient.GetDatabase("t1_frame");
            _collection = database.GetCollection<AuditLog>("AbpAuditLogs");

            // 创建无界通道（背压处理）
            _channel = Channel.CreateUnbounded<AuditLogInfo>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

            _cts = new CancellationTokenSource();
            _consumerTask = Task.Run(() => ConsumeAsync(_cts.Token));
        }

        public async Task SaveAsync(AuditLogInfo auditInfo)
        {
            // 只写入内存通道，立即返回（非阻塞）
            await _channel.Writer.WriteAsync(auditInfo);
        }

        private async Task ConsumeAsync(CancellationToken ct)
        {
            var batch = new List<AuditLog>(BatchSize);
            using var timer = new Timer(_ => { }, null, Timeout.Infinite, Timeout.Infinite);

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    // 等待数据或超时
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    cts.CancelAfter(MaxWaitMs);

                    var auditInfo = await _channel.Reader.ReadAsync(cts.Token);
                    var tmp = ConvertToAuditLog(auditInfo);
                    if (tmp != null)
                        batch.Add(tmp);

                    // 快速读取通道中剩余数据（非阻塞）
                    while (batch.Count < BatchSize && _channel.Reader.TryRead(out var item))
                    {
                        tmp = ConvertToAuditLog(item);
                        if (tmp != null)
                            batch.Add(tmp);
                    }

                    // 批量写入条件：批次满 或 超时
                    if (batch.Count >= BatchSize)
                    {
                        await FlushBatchAsync(batch);
                        batch.Clear();
                    }
                }
                catch (OperationCanceledException)
                {
                    // 超时触发刷新
                    if (batch.Count > 0)
                    {
                        await FlushBatchAsync(batch);
                        batch.Clear();
                    }
                }
            }

            // 退出前刷新剩余数据
            if (batch.Count > 0)
            {
                await FlushBatchAsync(batch);
            }
        }

        private async Task FlushBatchAsync(List<AuditLog> batch)
        {
            if (batch.Count == 0) return;

            try
            {
                // 使用无序批量写入（性能最优）
                await _collection.InsertManyAsync(batch, new InsertManyOptions
                {
                    IsOrdered = false,  // 无序写入，不停止在第一个错误
                    BypassDocumentValidation = true
                });
            }
            catch (MongoBulkWriteException ex)
            {
                // 部分失败处理：记录但继续
                // 实际生产环境需要记录到备用存储
                Console.WriteLine($"批量写入部分失败: {ex.WriteErrors.Count} 条");
            }
        }

        private AuditLog? ConvertToAuditLog(AuditLogInfo info)
        {
            var json = JsonConvert.SerializeObject(info);
            return JsonConvert.DeserializeObject<AuditLog>(json);          
        }

        public void Dispose()
        {
            _channel.Writer.Complete();
            _cts.Cancel();
            _consumerTask.Wait(TimeSpan.FromSeconds(5));
            _cts.Dispose();
        }
    }
}
