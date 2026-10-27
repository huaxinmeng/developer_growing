namespace t1_frame.application.abp.Seckill
{
    public sealed class SeckillOrderMessage
    {
        public long RunId { get; init; }

        public long ProductId { get; init; }

        public string RequestId { get; init; } = string.Empty;

        public int Quantity { get; init; }

        public int Bucket { get; init; }

        public DateTime OccurredAt { get; init; }
    }
}
