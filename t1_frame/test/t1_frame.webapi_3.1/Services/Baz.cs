using System;

namespace t1_frame.webapi_3._1
{
    public class Baz : Disposable, IBaz
    {
        ~Baz()
        {
            Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} {this.GetHashCode()} Baz.Finalize()");
        }

    }
}
