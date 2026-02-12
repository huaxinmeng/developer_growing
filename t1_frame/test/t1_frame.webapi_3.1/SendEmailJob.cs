
using Quartz;
using System;
using System.Threading.Tasks;
namespace t1_frame.webapi_3._1
{
    public class SendEmailJob : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            // Code that sends a periodic email to the user (for example)
            // Note: This method must always return a value 
            // This is especially important for trigger listeners watching job execution 
            Console.WriteLine($"time: {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} Hello, world!");

            return Task.CompletedTask;
        }
    }
}
