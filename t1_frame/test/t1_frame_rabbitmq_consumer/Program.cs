using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;

namespace t1_frame_rabbitmq_consumer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            #region  rabbitmq
            //var factory = new ConnectionFactory { HostName = "192.168.1.214", UserName = "nick", Password = "123" };
            //using var connection = factory.CreateConnection();
            //using var channel = connection.CreateModel();

            //channel.QueueDeclare(queue: "rpc_queue",
            //                     durable: false,
            //                     exclusive: false,
            //                     autoDelete: false,
            //                     arguments: null);
            //channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            ////channel.ExchangeDeclare(exchange: "topic_logs", type: ExchangeType.Topic);
            ////var queueName = channel.QueueDeclare().QueueName;
            ////if (args.Length < 1)
            ////{
            ////    Console.Error.WriteLine("Usage: {0} [binding_key...]",
            ////                            Environment.GetCommandLineArgs()[0]);
            ////    Console.WriteLine(" Press [enter] to exit.");
            ////    Console.ReadLine();
            ////    Environment.ExitCode = 1;
            ////    return;
            ////}

            ////channel.QueueBind(queue: queueName,
            ////                  exchange: "logs",
            ////                  routingKey: string.Empty);

            ////foreach (var bindingKey in args)
            ////{
            ////    channel.QueueBind(queue: queueName,
            ////                      exchange: "topic_logs",
            ////                      routingKey: bindingKey);
            ////}

            //Console.WriteLine(" [*] Waiting for messages.");

            //var consumer = new EventingBasicConsumer(channel);
            //consumer.Received += (model, ea) =>
            //{
            //    //var body = ea.Body.ToArray();
            //    //var message = Encoding.UTF8.GetString(body);
            //    //var routingKey = ea.RoutingKey;

            //    //Console.WriteLine($" [x] Received '{routingKey}':'{message}'");

            //    //int dots = message.Split('.').Length - 1;
            //    //Thread.Sleep(dots * 1000);

            //    //Console.WriteLine(" [x] Done");
            //    //channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);

            //    string response = string.Empty;

            //    var body = ea.Body.ToArray();
            //    var props = ea.BasicProperties;
            //    var replyProps = channel.CreateBasicProperties();
            //    replyProps.CorrelationId = props.CorrelationId;

            //    try
            //    {
            //        var message = Encoding.UTF8.GetString(body);
            //        int n = int.Parse(message);
            //        Console.WriteLine($" [.] Fib({message})");
            //        response = Fib(n).ToString();
            //    }
            //    catch (Exception e)
            //    {
            //        Console.WriteLine($" [.] {e.Message}");
            //        response = string.Empty;
            //    }
            //    finally
            //    {
            //        var responseBytes = Encoding.UTF8.GetBytes(response);
            //        channel.BasicPublish(exchange: string.Empty,
            //                             routingKey: props.ReplyTo,
            //                             basicProperties: replyProps,
            //                             body: responseBytes);
            //        channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            //    }
            //};
            ////channel.BasicConsume(queue: "task_queue",
            ////                     autoAck: false,
            ////                     consumer: consumer);
            //channel.BasicConsume(queue: "rpc_queue",
            //         autoAck: false,
            //         consumer: consumer);
            #endregion


            var conf = new ConsumerConfig
            {
                GroupId = "test-consumer-group",
                BootstrapServers = "192.168.1.102:9192",
                // Note: The AutoOffsetReset property determines the start offset in the event
                // there are not yet any committed offsets for the consumer group for the
                // topic/partitions of interest. By default, offsets are committed
                // automatically, so in this example, consumption will only start from the
                // earliest message in the topic 'my-topic' the first time you run the program.
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            using (var c = new ConsumerBuilder<Ignore, string>(conf).Build())
            {
                c.Subscribe("test-topic");

                CancellationTokenSource cts = new CancellationTokenSource();
                Console.CancelKeyPress += (_, e) => {
                    // Prevent the process from terminating.
                    e.Cancel = true;
                    cts.Cancel();
                };

                try
                {
                    while (true)
                    {
                        try
                        {
                            var cr = c.Consume(cts.Token);
                            Console.WriteLine($"Consumed message '{cr.Value}' at: '{cr.TopicPartitionOffset}'.");
                        }
                        catch (ConsumeException e)
                        {
                            Console.WriteLine($"Error occured: {e.Error.Reason}");
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Ensure the consumer leaves the group cleanly and final offsets are committed.
                    c.Close();
                }
            }

            Console.WriteLine(" Press [enter] to exit.");
            Console.ReadLine();

            static int Fib(int n)
            {
                if (n is 0 or 1)
                {
                    return n;
                }

                return Fib(n - 1) + Fib(n - 2);
            }
        }
    }
}