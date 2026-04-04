using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace t1_frame.core.abp
{
    public class CounterHelper
    {
        private static readonly CounterHelper _instance = new CounterHelper();
        private long _counter;
        private CounterHelper()
        {

        }

        public static CounterHelper Instance => _instance;

        public long Increment()
        {
            return Interlocked.Increment(ref _counter);
        }

        public long GetCount()
        {
            return Interlocked.Read(ref _counter);
        }
    }
}
