namespace t1_frame_worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    public Worker(ILogger<Worker> logger, IHostApplicationLifetime hostApplicationLifetime)
    {
        _logger = logger;
        _hostApplicationLifetime = hostApplicationLifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        //_logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
        //await Task.Delay(1000, stoppingToken);

        //_hostApplicationLifetime.StopApplication();
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }
            await Task.Delay(1000, stoppingToken);
        }
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        CancellationTokenSource cts = new CancellationTokenSource(10000);
        return base.StartAsync(cts.Token);
    }
}
