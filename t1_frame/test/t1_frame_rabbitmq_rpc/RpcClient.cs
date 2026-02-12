using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace t1_frame_rabbitmq_rpc
{
    public class RpcClient : IDisposable
    {
        private const string QUEUE_NAME = "rpc_queue";

        private readonly IConnection connection;
        private readonly IModel channel;
        private readonly string replyQueueName;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> callbackMapper = new();

        public RpcClient()
        {
            var factory = new ConnectionFactory { HostName = "192.168.1.214", UserName = "nick", Password = "123" };

            connection = factory.CreateConnection();
            channel = connection.CreateModel();
            // declare a server-named queue
            replyQueueName = channel.QueueDeclare().QueueName;
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                if (!callbackMapper.TryRemove(ea.BasicProperties.CorrelationId, out var tcs))
                    return;
                var body = ea.Body.ToArray();
                var response = Encoding.UTF8.GetString(body);
                tcs.TrySetResult(response);
            };

            channel.BasicConsume(consumer: consumer,
                                 queue: replyQueueName,
                                 autoAck: true);
        }

        public Task<string> CallAsync(string message, CancellationToken cancellationToken = default)
        {
            IBasicProperties props = channel.CreateBasicProperties();
            var correlationId = Guid.NewGuid().ToString();
            props.CorrelationId = correlationId;
            props.ReplyTo = replyQueueName;
            var messageBytes = Encoding.UTF8.GetBytes(message);
            var tcs = new TaskCompletionSource<string>();
            callbackMapper.TryAdd(correlationId, tcs);

            channel.BasicPublish(exchange: string.Empty,
                                 routingKey: QUEUE_NAME,
                                 basicProperties: props,
                                 body: messageBytes);

            cancellationToken.Register(() => callbackMapper.TryRemove(correlationId, out _));
            return tcs.Task;
        }

        public void Dispose()
        {
            connection.Close();
        }
    }
}
