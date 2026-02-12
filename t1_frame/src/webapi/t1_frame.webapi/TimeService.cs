
using Microsoft.Extensions.Logging;
using t1_frame.core.TaskScheduler;

namespace t1_frame.webapi
{
    public class TimeService : IHostedService, IAsyncDisposable
    {
        private readonly Task _completedTask = Task.CompletedTask;
        private int _executionCount = 0;
        private Timer? _timer;
        private readonly ILogger<TimeService> logger;
        private readonly ScheduledTaskFactory taskFactory;
        //private readonly TestConsumer consumer;
        private readonly IServiceProvider _serviceProvider;
        public TimeService(ILogger<TimeService> _logger, ScheduledTaskFactory _taskFactory, TestConsumer _consumer, IServiceProvider serviceProvider) {
            logger = _logger;
            taskFactory = _taskFactory;
            _serviceProvider = serviceProvider;
        }
        public async Task StartAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("{Service} is running.", nameof(TimeService));
            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(2));

            //return _completedTask;
        }

        private async void DoWork(object? state)
        {
            int count = Interlocked.Increment(ref _executionCount);

            //logger.LogInformation(
            //    "{Service} is working, execution count: {Count:#,0}",
            //    nameof(TimeService),
            //    count);

            var consumer = _serviceProvider.GetRequiredService<TestConsumer>();
            var action = typeof(TestConsumer).GetMethod("HandBillExportTask")?.CreateDelegate<Func<Task>>(consumer);
            var task1 = taskFactory.CreateTask(action);
            await task1.CreateAndRun();
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation(
                "{Service} is stopping.", nameof(TimeService));

            _timer?.Change(Timeout.Infinite, 0);

            return _completedTask;
        }

        public async ValueTask DisposeAsync()
        {
            if (_timer is IAsyncDisposable timer)
            {
                await timer.DisposeAsync();
            }

            _timer = null;
        }
    }
}
