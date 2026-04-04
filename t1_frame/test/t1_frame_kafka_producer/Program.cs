using Confluent.Kafka;
using System.Text;
using System.Threading.Channels;

namespace t1_frame_kafka_producer
{
    internal class Program
    {
        private static volatile bool isComplete = true;
        static async Task Main(string[] args)
        {
            var config = new ProducerConfig { BootstrapServers = "192.168.3.215:9192" };
            var routingKey = (args.Length > 0) ? args[0] : "test-topic";
            using var p = new ProducerBuilder<Null, string>(config).Build();
            // If serializers are not specified, default serializers from
            // `Confluent.Kafka.Serializers` will be automatically used where
            // available. Note: by default strings are encoded as UTF8.
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
                await Task.Run(async () =>
                {
                    // var routingKey = (args.Length > 0) ? args[0] : "anonymous.info";
                    //var message = (args.Length > 1)
                    //              ? string.Join(" ", args.Skip(1).ToArray())
                    //              : "Hello World!";
                    // var routingKey = "rpc_queue";

                    var message = str;
                    try
                    {
                        var dr = await p.ProduceAsync(routingKey, new Message<Null, string> { Value = message });
                        Console.WriteLine($"Delivered '{dr.Value}' to '{dr.TopicPartitionOffset}'");
                    }
                    catch (ProduceException<Null, string> e)
                    {
                        Console.WriteLine($"Delivery failed: {e.Error.Reason}");
                    }
                    Console.WriteLine($" [x] Sent '{routingKey}':'{message}'");
                    Console.WriteLine("等待输入...");
                }).ContinueWith(t => {
                    isComplete = true;
                });
            }


            Console.WriteLine(" Press [enter] to exit.");
            Console.ReadLine();
        }
    }
}
