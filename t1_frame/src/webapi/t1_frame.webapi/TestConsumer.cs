using t1_frame.core.TaskScheduler;

namespace t1_frame.webapi
{
    public class TestConsumer
    {
        private readonly RabbitMQService service;
        public TestConsumer(RabbitMQService _service)
        {
            service = _service;
        }
        public async Task HandBillExportTask()
        {
            Console.WriteLine($"time {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
            var msg = service.TryGet("t1_test");

            await Task.CompletedTask;
        }


    }
}
