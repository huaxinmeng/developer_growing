using System;

namespace t1_frame.webapi_3._1
{
    public class Foo : Disposable, IFoo
    {
        ~Foo()
        {
            Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} {this.GetHashCode()}  Foo.Finalize()");
        }
    }
}
