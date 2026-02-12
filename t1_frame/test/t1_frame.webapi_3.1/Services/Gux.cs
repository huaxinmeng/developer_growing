using System;

namespace t1_frame.webapi_3._1
{
    public class Gux : IGux
    {
        public IFoo Foo { get; private set; }

        public IBar Bar { get; private set; }

        public IBaz Baz { get; private set; }

        public Gux(IFoo foo, IBar bar, IBaz baz) : this (foo, bar)
        {
            this.Foo = foo;
            this.Bar = bar;
            this.Baz = baz;

            Console.WriteLine("Gux(IFoo, IBar, IBaz)");
        }

        public Gux(IFoo foo, IBar bar)
        {
            Console.WriteLine("Gux(IFoo, IBar)");
        }

        //public Gux(IFoo foo, IBaz bar)
        //{
        //    Console.WriteLine("Gux(IFoo, IBaz)");
        //}
    }
}
