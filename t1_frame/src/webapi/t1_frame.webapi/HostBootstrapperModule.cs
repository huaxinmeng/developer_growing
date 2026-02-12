using t1_frame.core;
using t1_frame.log;

namespace t1_frame.webapi
{
    [DependsOn(typeof(NfpLogModule))]
    public class HostBootstrapperModule : NfpModule
    {
        public override void Initialize()
        {
            base.Initialize();
        }
    }
}
