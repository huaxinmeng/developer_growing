using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace t1_frame_masstransit
{
    public class TaskSample
    {
        /// <summary>
        /// 箱单本地文件地址
        /// </summary>
        public string box_urlname { get; set; }

        /// <summary>
        /// 袋子数
        /// </summary>
        public int bag_count { get; set; }

        public async Task TestAsync(int value)
        {
            //await Task.Delay(2000);
            //await Task.CompletedTask;
            Console.WriteLine($"{DateTime.Now.ToString("yyyy_MM-dd HH:mm:ss.ffffff")} TestAsync{value} start! {Thread.CurrentThread.ManagedThreadId}");
            //foreach (var item in Enumerable.Range(0, 1000000000))
            //{

            //}
            try
            {
                await Task.Yield();
                await CountAsync(value);

               
            }
            catch (Exception ex)
            {

            }
            
            //await Task.Delay(2000);
            Console.WriteLine($"{DateTime.Now.ToString("yyyy_MM-dd HH:mm:ss.ffffff")} TestAsync{value} end! {Thread.CurrentThread.ManagedThreadId}");
            //return Task.FromResult(100);

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<int> CountAsync(int value)
        {
            //await Task.CompletedTask;
            foreach (var item in Enumerable.Range(0, 1000000000))
            {
                // void 异常无法捕捉
                if (item > value)
                {
                    //throw new NotImplementedException();
                    //await Task.Yield();
                }

                
            }

            Console.WriteLine($"{DateTime.Now.ToString("yyyy_MM-dd HH:mm:ss.ffffff")} CountAsync{value}! {Thread.CurrentThread.ManagedThreadId}");
            await Task.CompletedTask;
            return 0;
        }
    }
}
