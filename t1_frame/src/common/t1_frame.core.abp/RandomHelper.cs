using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace t1_frame.core.abp
{
    public class RandomHelper
    {
        private static readonly RandomHelper _instance = new RandomHelper();
        private Random _random = new Random();
        private RandomHelper()
        {

        }

        public static RandomHelper Instance => _instance;

        public Random GetRandomCtl() => _random;

        
    }
}
