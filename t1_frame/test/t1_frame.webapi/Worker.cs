using MassTransit;

namespace t1_frame.webapi
{
    public class Worker : BackgroundService
    {
        readonly IBus _bus;

        public Worker(IBus bus)
        {
            _bus = bus;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await _bus.Publish(new GettingStarted { Value = $"The time is {DateTimeOffset.Now}" }, stoppingToken);

                await Task.Delay(1000, stoppingToken);
            }
        }
    }

    public record GettingStarted()
    {
        public string Value { get; init; }
    }
}
