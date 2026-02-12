using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace t1_frame_console
{
    public interface ITest
    {
        int GetStr(string node);
    }


    interface ICovariant<out R>
    {
        void DoSomething(Action<R> callback);

        //void DoSomething<T>() where T : R;
    }

    // The type T is covariant.  
    public delegate T DVariant<out T>();

    // The type T is invariant.  
    public delegate T DInvariant<T>();
}
