using System;
using System.Text.RegularExpressions;

namespace demo
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World! This is span_demo（test2）!");

            int a = -1;
            int b = 1;
            //Console.WriteLine($"a:UTC{a.ToString("00")}:00");
            //Console.WriteLine($"b:UTC{b.ToString("00")}:00");

            //var targ = Convert.ToInt32("-3");
            var val = System.TimeZone.CurrentTimeZone.GetUtcOffset(System.DateTime.Now).Hours;

            var ll = TimeZoneInfo.Local.BaseUtcOffset.Hours;
            Console.WriteLine($"{ToTimeZoneFormat(2)}");
            Console.WriteLine($"{ToTimeZoneFormat(0)}");
            Console.WriteLine($"{ToTimeZoneFormat(-5)}");
            Console.ReadKey();
        }

        public static string ToTimeZoneFormat(int source)
        {
            return $"UTC{(source < 0 ? source.ToString() : ("+" + source.ToString()))}";
        }
    }
}
