namespace t1_frame.application.abp.Seckill
{
    public sealed class SeckillKafkaOptions
    {
        public string BootstrapServers { get; set; } = "localhost:9092";

        public string Topic { get; set; } = "seckill-orders";

        public string GroupId { get; set; } = "seckill-order-worker";

        public int Partitions { get; set; } = 8;

        public short ReplicationFactor { get; set; } = 1;
    }
}
