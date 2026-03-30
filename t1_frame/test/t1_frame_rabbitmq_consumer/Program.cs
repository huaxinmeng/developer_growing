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
            var factory = new ConnectionFactory { HostName = "192.168.3.214", UserName = "nick", Password = "123" };
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();
            var routingKey = (args.Length > 0) ? args[0] : "t1_test";
            var exchange = routingKey ?? "topic_logs";
            //channel.QueueDeclare(queue: routingKey,
            //                     durable: false,
            //                     exclusive: false,
            //                     autoDelete: false,
            //                     arguments: null);

            if (args.Length > 1 && args[1] == "fanout")
            {
                channel.ExchangeDeclare(exchange: exchange, type: ExchangeType.Fanout);
            }
            else if (args.Length > 1 && args[1] == "direct")
            {
                channel.ExchangeDeclare(exchange: exchange, type: ExchangeType.Direct);
            }
            else if (args.Length > 1 && args[1] == "topic")
            {
                channel.ExchangeDeclare(exchange: exchange, type: ExchangeType.Topic);
            }
            else if (args.Length > 1 && args[1] == "headers")
            {
                channel.ExchangeDeclare(exchange: exchange, type: ExchangeType.Headers);
            }

            // channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            //channel.ExchangeDeclare(exchange: "topic_logs", type: ExchangeType.Topic);
            var queueName = channel.QueueDeclare().QueueName;
            if (args.Length <= 1)
            {
                Console.Error.WriteLine("Usage: {0} [binding_key...]",
                                        Environment.GetCommandLineArgs()[0]);
                Console.WriteLine(" Press [enter] to exit.");
                Console.ReadLine();
                Environment.ExitCode = 1;
                return;
            }

            if (args.Length > 1 && args[1] == "fanout")
            {
                channel.QueueBind(queue: queueName,
                                  exchange: exchange,
                                  routingKey: string.Empty);
            }
            else
            {
                var index = 0;
                foreach (var bindingKey in args)
                {
                    if (index <= 1)
                    {
                        index++;
                        continue;
                    }

                    channel.QueueBind(queue: queueName,
                                      exchange: exchange,
                                      routingKey: bindingKey);
                    index++;
                }
            }

            Console.WriteLine(" [*] Waiting for messages.");

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                // var routingKey = ea.RoutingKey;

                Console.WriteLine($" [x] Received '{ea.RoutingKey}':'{message}'");

                int dots = message.Split('.').Length - 1;
                Thread.Sleep(dots * 1000);

                Console.WriteLine(" [x] Done");
                channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);

                //string response = string.Empty;

                //var body = ea.Body.ToArray();
                //var props = ea.BasicProperties;
                //var replyProps = channel.CreateBasicProperties();
                //replyProps.CorrelationId = props.CorrelationId;

                //try
                //{
                //    var message = Encoding.UTF8.GetString(body);
                //    int n = int.Parse(message);
                //    Console.WriteLine($" [.] Fib({message})");
                //    response = Fib(n).ToString();
                //}
                //catch (Exception e)
                //{
                //    Console.WriteLine($" [.] {e.Message}");
                //    response = string.Empty;
                //}
                //finally
                //{
                //    var responseBytes = Encoding.UTF8.GetBytes(response);
                //    channel.BasicPublish(exchange: string.Empty,
                //                         routingKey: props.ReplyTo,
                //                         basicProperties: replyProps,
                //                         body: responseBytes);
                //    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                //}
            };
            //channel.BasicConsume(queue: "task_queue",
            //                     autoAck: false,
            //                     consumer: consumer);

            routingKey = queueName;
            channel.BasicConsume(queue: routingKey,
                     autoAck: false,
                     consumer: consumer);
            #endregion


            //var conf = new ConsumerConfig
            //{
            //    GroupId = "test-consumer-group",
            //    BootstrapServers = "192.168.1.102:9192",
            //    // Note: The AutoOffsetReset property determines the start offset in the event
            //    // there are not yet any committed offsets for the consumer group for the
            //    // topic/partitions of interest. By default, offsets are committed
            //    // automatically, so in this example, consumption will only start from the
            //    // earliest message in the topic 'my-topic' the first time you run the program.
            //    AutoOffsetReset = AutoOffsetReset.Earliest
            //};

            //using (var c = new ConsumerBuilder<Ignore, string>(conf).Build())
            //{
            //    c.Subscribe("test-topic");

            //    CancellationTokenSource cts = new CancellationTokenSource();
            //    Console.CancelKeyPress += (_, e) => {
            //        // Prevent the process from terminating.
            //        e.Cancel = true;
            //        cts.Cancel();
            //    };

            //    try
            //    {
            //        while (true)
            //        {
            //            try
            //            {
            //                var cr = c.Consume(cts.Token);
            //                Console.WriteLine($"Consumed message '{cr.Value}' at: '{cr.TopicPartitionOffset}'.");
            //            }
            //            catch (ConsumeException e)
            //            {
            //                Console.WriteLine($"Error occured: {e.Error.Reason}");
            //            }
            //        }
            //    }
            //    catch (OperationCanceledException)
            //    {
            //        // Ensure the consumer leaves the group cleanly and final offsets are committed.
            //        c.Close();
            //    }
            //}

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