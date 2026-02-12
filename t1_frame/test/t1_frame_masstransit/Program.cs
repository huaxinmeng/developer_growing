namespace t1_frame_masstransit
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine($"{DateTime.Now.ToString("yyyy_MM-dd HH:mm:ss.ffffff")} come to start! {Thread.CurrentThread.ManagedThreadId}");

            //var tskObj = new TaskSample();

            var tskObj = default(TaskSample);
            //IEnumerable<Task> tasks = new List<Task>();
            //IEnumerable<Task> tasks = Enumerable.Range(0, 10).Select(t => tskObj.TestAsync(t)).ToList();

            //await new TaskSample().TestAsync();

            //var val1 = tskObj.TestAsync(1);
            //var val2 = tskObj.TestAsync(2);
            //await Task.Delay(2000);
            //var context = SynchronizationContext.Current;
            //tskObj.TestAsync(1);
            //tskObj.TestAsync(2);

            //Task.Run(() =>
            //{
            //    var val1 = tskObj.TestAsync(1);
            //});
            //await Task.Run(() =>
            //{
            //    var val2 = tskObj.TestAsync(2);
            //});

            // Task.Factory.StartNew(async () =>
            //{
            //     tskObj.TestAsync(1);
            //});

            // Task.Factory.StartNew(async () =>
            //{
            //     tskObj.TestAsync(2);
            //});

            //tskObj.TestAsync(1).GetAwaiter().GetResult();
            //tskObj.TestAsync(2).GetAwaiter().GetResult();
            //await val2;
            //await val1;
            //await Task.WhenAll(val2, val1);

            Console.WriteLine($"{DateTime.Now.ToString("yyyy_MM-dd HH:mm:ss.ffffff")} come to end! {Thread.CurrentThread.ManagedThreadId}");
            await Task.CompletedTask;
            Console.ReadKey();
        }
    }
}