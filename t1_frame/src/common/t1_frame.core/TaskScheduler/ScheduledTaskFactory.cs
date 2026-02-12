using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace t1_frame.core.TaskScheduler
{
    public class ScheduledTaskFactory
    {
        public ScheduledTask CreateTask(Func<Task> action)
        {
            return action;
        }
    }

}
