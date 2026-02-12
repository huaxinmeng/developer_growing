using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace t1_frame.core.TaskScheduler
{
    public class RabbitMQService
    {
        private readonly ConnectionFactory _factory;
        /// <summary>
        /// 获取 RabbitMQ Channel 对象
        /// </summary>
        public IModel channel { get; private set; }
        public RabbitMQService(ILogger<RabbitMQService> _logger)
        {
            _factory = new ConnectionFactory { HostName = "192.168.1.214", UserName = "nick", Password = "123" };
            var connection = _factory.CreateConnection();
            channel = connection.CreateModel();
            channel.QueueDeclare(queue: "t1_test",
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);
        }

        public async Task TryGet(string queueName)
        {
            await Task.CompletedTask;
            bool autoAck = false;
            BasicGetResult result = channel.BasicGet(queueName, autoAck);
            if (result == null)
            {
                // No message available at this time.
                Console.WriteLine("No message available at this time");
            }
            else
            {
                IBasicProperties props = result.BasicProperties;
                ReadOnlyMemory<byte> body = result.Body;
                var message = Encoding.UTF8.GetString(body.ToArray());
                var routingKey = result.RoutingKey;

                Console.WriteLine($" [x] Received '{routingKey}':'{message}'");

                channel.BasicAck(result.DeliveryTag, false);
            }


        }

    }
}
