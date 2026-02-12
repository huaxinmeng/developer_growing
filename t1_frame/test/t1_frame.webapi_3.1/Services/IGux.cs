using System;

namespace t1_frame.webapi_3._1
{
    public interface IGux
    {
        IFoo Foo { get; }

        IBar Bar { get; }

        IBaz Baz { get; }
    }
}
