namespace t1_frame.application.abp.Seckill
{
    public class SeckillResultDto
    {
        public bool Accepted { get; set; }

        public string RequestId { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public int Bucket { get; set; }

        public string Message { get; set; } = string.Empty;
    }

    public class SeckillStatusDto
    {
        public string RequestId { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;
    }

    public class SeckillBucketDto
    {
        public int Bucket { get; set; }

        public int Stock { get; set; }
    }

    public class SeckillStatsDto
    {
        public long RunId { get; set; }

        public long ProductId { get; set; }

        public int TotalStock { get; set; }

        public long Accepted { get; set; }

        public long Created { get; set; }

        public long Failed { get; set; }

        public long Pending { get; set; }

        /// <summary>
        /// Redis List outbox + processing 中尚未确认投递 Kafka 的条目数。
        /// </summary>
        public long OutboxLength { get; set; }

        public List<SeckillBucketDto> Buckets { get; set; } = new();
    }

    public class SeckillResetDto
    {
        public long RunId { get; set; }

        public long ProductId { get; set; }

        public int TotalStock { get; set; }

        public int BucketCount { get; set; }
    }
}
