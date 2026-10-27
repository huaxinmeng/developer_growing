namespace t1_frame.application.abp.Seckill
{
    /// <summary>
    /// 秒杀指标计数器。生产链路的队列由 Redis List outbox + Kafka 承担。
    /// </summary>
    public sealed class SeckillQueue
    {
        private long _accepted;
        private long _created;
        private long _failed;

        public long Accepted => Interlocked.Read(ref _accepted);

        public long Created => Interlocked.Read(ref _created);

        public long Failed => Interlocked.Read(ref _failed);

        public long Pending => Math.Max(0, Accepted - Created - Failed);

        public void MarkAccepted()
        {
            Interlocked.Increment(ref _accepted);
        }

        public void MarkCreated(int count)
        {
            Interlocked.Add(ref _created, count);
        }

        public void MarkFailed(int count)
        {
            Interlocked.Add(ref _failed, count);
        }

        public void ResetCounters()
        {
            Interlocked.Exchange(ref _accepted, 0);
            Interlocked.Exchange(ref _created, 0);
            Interlocked.Exchange(ref _failed, 0);
        }
    }
}
