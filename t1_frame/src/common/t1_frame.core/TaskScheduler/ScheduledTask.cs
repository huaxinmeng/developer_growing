using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace t1_frame.core.TaskScheduler
{
    public abstract class ScheduledTask
    {
        /// <summary>
        /// 创建并执行任务
        /// </summary>
        /// <returns>创建的任务</returns>
        public abstract Task CreateAndRun();

        /// <summary>
        /// 定义从异步方法到 ScheduledTask 的隐式类型转换
        /// </summary>
        /// <param name="asyncAction">要异步执行的方法</param>
        public static implicit operator ScheduledTask(Func<Task> asyncAction)
        {
            return new AsyncScheduledTask(asyncAction);
        }

        private class AsyncScheduledTask : ScheduledTask
        {
            private readonly Func<Task> _action;

            public AsyncScheduledTask(Func<Task> action)
            {
                _action = action;
            }

            public override Task CreateAndRun()
            {
                return Task.Run(_action);
            }
        }
    }
}
