using System;

namespace t1_frame.webapi_3._1
{
    public interface IFoobar<T1, T2>
    {
        T1 Foo { get; }
        T2 Bar { get; }
    }
}
