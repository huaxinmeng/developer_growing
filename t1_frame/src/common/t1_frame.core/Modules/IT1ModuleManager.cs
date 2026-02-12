using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace t1_frame.core
{
    public interface IT1ModuleManager
    {
        T1ModuleInfo StartupModule { get; }

        IReadOnlyList<T1ModuleInfo> Modules { get; }

        void Initialize(Type startupModule);

        void StartModules();

        void ShutdownModules();
    }
}
