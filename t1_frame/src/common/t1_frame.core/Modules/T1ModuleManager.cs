using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace t1_frame.core
{
    public class T1ModuleManager : IT1ModuleManager
    {
        public T1ModuleInfo StartupModule => throw new NotImplementedException();

        public IReadOnlyList<T1ModuleInfo> Modules => throw new NotImplementedException();

        public void Initialize(Type startupModule)
        {
            throw new NotImplementedException();
        }

        public void ShutdownModules()
        {
            throw new NotImplementedException();
        }

        public void StartModules()
        {
            throw new NotImplementedException();
        }
    }
}
