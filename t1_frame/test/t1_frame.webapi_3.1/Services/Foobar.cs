using System;

namespace t1_frame.webapi_3._1
{
    public class Foobar<T1, T2> : IFoobar<T1, T2>
    {
        public T1 Foo { get; private set; }
        public T2 Bar { get; private set; }

        public Foobar(T1 foo, T2 bar)
        {
            Foo = foo;
            Bar = bar;
        }
    }
}
