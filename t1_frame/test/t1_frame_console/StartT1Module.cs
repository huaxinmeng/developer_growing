using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using t1_frame.core;
using t1_frame_library;

namespace t1_frame_console
{
    [DependsOn(typeof(SimpleT1Module))]
    public class StartT1Module : NfpModule
    {
        public override void Initialize()
        {
            base.Initialize();
        }
    }

    public class SimpleStartT1Module : StartT1Module
    {
        public override void Initialize()
        {
            base.Initialize();
        }
    }
}
