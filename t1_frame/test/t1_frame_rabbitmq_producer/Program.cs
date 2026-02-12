using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using System.Text;

namespace t1_frame_rabbitmq_producer
{
    internal class Program
    {
        private static volatile bool isComplete = true;
        static async Task Main(string[] args)
        {
            #region rabbitmq
            var factory = new ConnectionFactory { HostName = "192.168.1.214", UserName = "nick", Password = "123" };
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.QueueDeclare(queue: "t1_test",
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);
            //channel.ExchangeDeclare(exchange: "topic_logs", type: ExchangeType.Topic);

            //var message = GetMessage(args);
            Console.WriteLine("等待输入...");
            var flag = true;
            while (flag)
            {
                string str = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(str)) continue;
                if (str.ToLower() == "exit")
                {
                    flag = false;
                    break;
                }

                if (str.ToLower() == "clear")
                {
                    Console.Clear();
                    continue;
                }

                if (!isComplete)
                {
                    Console.WriteLine("上一次任务未处理完，请等待...");
                    continue;
                }

                isComplete = false;
                await Task.Run(() =>
                {
                    //var routingKey = (args.Length > 0) ? args[0] : "anonymous.info";
                    //var message = (args.Length > 1)
                    //              ? string.Join(" ", args.Skip(1).ToArray())
                    //              : "Hello World!";
                    var routingKey = "";
                    var message = str;
                    var body = Encoding.UTF8.GetBytes(message);
                    var properties = channel.CreateBasicProperties();
                    properties.Persistent = true;

                    channel.BasicPublish(exchange: string.Empty,
                                         routingKey: "t1_test",
                                         basicProperties: properties,
                                         body: body);
                    Console.WriteLine($" [x] Sent '{routingKey}':'{message}'");
                    Console.WriteLine("等待输入...");
                }).ContinueWith(t => {
                    isComplete = true;
                });
            }

          
            //channel.BasicPublish(exchange: "topic_logs",
            //         routingKey: routingKey,
            //         basicProperties: null,
            //         body: body);
            
            #endregion

            //var config = new ProducerConfig { BootstrapServers = "192.168.1.102:9192" };

            //// If serializers are not specified, default serializers from
            //// `Confluent.Kafka.Serializers` will be automatically used where
            //// available. Note: by default strings are encoded as UTF8.
            //using (var p = new ProducerBuilder<Null, string>(config).Build())
            //{
            //    try
            //    {
            //        var dr = await p.ProduceAsync("test-topic", new Message<Null, string> { Value = "test" });
            //        Console.WriteLine($"Delivered '{dr.Value}' to '{dr.TopicPartitionOffset}'");
            //    }
            //    catch (ProduceException<Null, string> e)
            //    {
            //        Console.WriteLine($"Delivery failed: {e.Error.Reason}");
            //    }
            //}

            Console.WriteLine(" Press [enter] to exit.");
            Console.ReadLine();
        }
        static string GetMessage(string[] args)
        {
            return ((args.Length > 0) ? string.Join(" ", args) : "Hello World!");
        }
    }
}