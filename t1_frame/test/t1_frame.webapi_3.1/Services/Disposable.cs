using System;

namespace t1_frame.webapi_3._1
{
    public class Disposable : IDisposable
    {
        public void Dispose()
        {
            Console.WriteLine("{1}: {2} {0}.Dispose()", this.GetType(), DateTime.Now.ToString("HH:mm:ss"), this.GetHashCode());

            //GC.Collect();
        }
    }
}
