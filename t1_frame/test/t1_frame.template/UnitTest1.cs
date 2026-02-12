//[assembly: RequiresThread]

using System.Collections;

namespace t1_frame.template;

//[TestFixture, Apartment(ApartmentState.MTA)]
//[Parallelizable(scope: ParallelScope.All)]
//[TestFixture(typeof(int))]
[TestFixture(typeof(double))]
public class Tests<T>
{
    [SetUp]
    public void Setup()
    {
        Console.WriteLine($"time:{DateTime.Now.ToString("hh:mm:ss.fffffff")} Tests Setup");
    }

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

    [TearDown]
    public void TearDown()
    {
        /* ... */
        Console.WriteLine("Tests TearDown");
    }

    [Test, Combinatorial, Apartment(ApartmentState.MTA), Order(1)]
    [Author("Joe Developer")]
    [Category("Long")]
    public void MyTest(
    [Values(1, 2, 3)] int x,
    [Values("A", "B")] string s)
    {
        Console.WriteLine($"time:{DateTime.Now.ToString("hh:mm:ss.fffffff")} x:{x} s:{s}");
    }

    [Test, Description("Test description here"),
        //MaxTime(2000),
        Order(1), Property("Severity", "Critical"), Timeout(2000)]
    [Category("Long")]
    //[Culture(Exclude = "en,de")]
    //[SetCulture("fr-FR")]
    //[SetUICulture("fr-FR")]
    [Ignore("Waiting for Joe to fix his bugs", Until = "2023-05-03 12:00:00Z")]
    //[Platform(Exclude = "Windows10,WinME")]
    [Retry(3)]
    public void Test1()
    {
        Thread.Sleep(2500);

        Console.WriteLine($"time:{DateTime.Now.ToString("hh:mm:ss.fffffff")} Test1");
        Assert.Pass();
    }

    [Test, Pairwise]
    public void MyTestPairwise(
    [Values("a", "b", "c")] string a,
    [Values("+", "-")] string b,
    [Values("x", "y")] string c)
    {
        Console.WriteLine("{0} {1} {2}", a, b, c);
    }

    [Test]
    public void MyTestRandom(
    [Values(1, 2, 3)] int x,
    [Random(-1.0, 1.0, 5)] double d)
    {
        Console.WriteLine($"time:{DateTime.Now.ToString("hh:mm:ss.fffffff")} x:{x} d:{d}");
    }

    [Test]
    public void MyTestRange(
    [Values(1, 2, 3)] int x,
    [Range(0.2, 0.6, 0.2)] double d)
    {
        /* ... */
        Console.WriteLine($"time:{DateTime.Now.ToString("hh:mm:ss.fffffff")} x:{x} d:{d}");
    }

    [Test]
    [Repeat(25)]
    public void MyTestRepeat()
    {
        /* The contents of this test will be run 25 times. */
    }

    [Test, Sequential]
    public void MyTestSequential(
    [Values(1, 2, 3)] int x,
    [Values("A", "B")] string s)
    {
        /* ... */
        Console.WriteLine($"time:{DateTime.Now.ToString("hh:mm:ss.fffffff")} x:{x} s:{s}");
    }

    [TestCase(12, 3, 4)]
    [TestCase(12, 2, 6)]
    [TestCase(12, 4, 3)]
    public void DivideTest(int n, int d, int q)
    {
        Assert.That(n / d, Is.EqualTo(q));
    }

    [TestCaseSource(nameof(DivideCases))]
    public void DivideTestCaseSource(int n, int d, int q)
    {
        Assert.AreEqual(q, n / d);
    }

    public static object[] DivideCases =
    {
        new object[] { 12, 3, 4 },
        new object[] { 12, 2, 6 },
        new object[] { 12, 4, 3 }
    };

    [TestCaseSource(nameof(TestStrings), new object[] { true })]
    public void LongNameWithEvenNumberOfCharacters(string name)
    {
        Assert.That(name.Length, Is.GreaterThan(5));

        bool hasEvenNumOfCharacters = (name.Length % 2) == 0;
        Assert.That(hasEvenNumOfCharacters, Is.True);
    }

    [TestCaseSource(nameof(TestStrings), new object[] { false })]
    public void ShortNameWithEvenNumberOfCharacters(string name)
    {
        Assert.That(name.Length, Is.LessThan(15));

        bool hasEvenNumOfCharacters = (name.Length % 2) == 0;
        Assert.That(hasEvenNumOfCharacters, Is.True);
    }

    private static IEnumerable<string> TestStrings(bool generateLongTestCase)
    {
        if (generateLongTestCase)
        {
            yield return "ThisIsAVeryLongNameThisIsAVeryLongName";
            yield return "SomeName";
            yield return "YetAnotherName";
        }
        else
        {
            yield return "AA";
            yield return "BB";
            yield return "CC";
        }
    }

    [TestCaseSource(typeof(AnotherClassWithTestFixtures), nameof(AnotherClassWithTestFixtures.DivideCases))]
    public void DivideTestAnotherClassWithTestFixtures(int n, int d, int q)
    {
        Assert.AreEqual(q, n / d);
    }

    [TestCaseSource(typeof(DivideCasesClass))]
    public void DivideTestDivideCasesClass(int n, int d, int q)
    {
        Assert.AreEqual(q, n / d);
    }

    private static int[] _evenNumbers = { 2, 4, 6, 8 };

    [Test, TestCaseSource(nameof(_evenNumbers))]
    public void TestMethod(int num)
    {
        Assert.That(num % 2, Is.Zero);
    }

    //[DatapointSource]
    //public double[] values = new double[] { 0.0, 1.0, -1.0, 42.0 };

    //[Theory]
    //public void SquareRootDefinition(double num)
    //{
    //    Assume.That(num >= 0.0);

    //    double sqrt = Math.Sqrt(num);

    //    Assert.That(sqrt >= 0.0);
    //    Assert.That(sqrt * sqrt, Is.EqualTo(num).Within(0.000001));
    //}

    [Datapoint]
    public double[] ArrayDouble1 = { 1.2, 3.4 };

    [Datapoint]
    public double[] ArrayDouble2 = { 5.6, 7.8 };

    [Datapoint]
    public int[] ArrayInt = { 0, 1, 2, 3 };

    [Theory]
    public void TestGenericForArbitraryArray(T[] array)
    {
        Assert.That(array.Length, Is.EqualTo(4));
    }

    [TestCaseSource(typeof(MyDataClass), nameof(MyDataClass.TestCases))]
    public int DivideTestTestCaseData(int n, int d)
    {
        return n / d;
    }
}

public class MyDataClass
{
    public static IEnumerable TestCases
    {
        get
        {
            yield return new TestCaseData(12, 3).Returns(4);
            yield return new TestCaseData(12, 2).Returns(6);
            yield return new TestCaseData(12, 4).Returns(3);
        }
    }
}

public class AnotherClassWithTestFixtures
{
    public static object[] DivideCases =
    {
        new object[] { 12, 3, 4 },
        new object[] { 12, 2, 6 },
        new object[] { 12, 4, 3 }
    };
}

public class DivideCasesClass : IEnumerable
{
    public IEnumerator GetEnumerator()
    {
        yield return new object[] { 12, 3, 4 };
        yield return new object[] { 12, 2, 6 };
        yield return new object[] { 12, 4, 3 };
    }
}