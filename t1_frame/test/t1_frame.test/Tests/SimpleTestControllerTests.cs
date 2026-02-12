using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

//[assembly: NonTestAssembly]

namespace t1_frame.Test
{
    [TestFixture, Explicit]
    public class SimpleTestControllerTests : AppTestBase
    {
        //private IDapApiBaseCore _core;
        //public SimpleTestControllerTests()
        //{
        //    //_core = core;
        //}
        [OneTimeSetUp]
        public void Init()
        {
            Console.WriteLine("Tests Init");
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            /* ... */
            Console.WriteLine("Tests Cleanup");
        }

        [Test]
        public void Should_Resolve_Controller()
        {
            var factory = ServiceProvider.GetService<IHttpClientFactory>();
            Assert.IsNotNull(factory);
            //throw new NotImplementedException();
        }
    }
}