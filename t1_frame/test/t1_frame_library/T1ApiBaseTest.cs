using System.Diagnostics.CodeAnalysis;

namespace t1_frame_library
{
    public partial class T1ApiBaseTest : IEntity
    {
        public T1ApiBaseTest()
        {
            record_field = new Person("xiao", "hong");
        }

        public DateTime create_on { get; set; }
        public byte byte_field { get; set; }
        public sbyte sbyte_field { get; set; }
        public short short_field { get; set; }
        public ushort ushort_field { get; set; }
        public int int_field { get; set; }
        public uint uint_field { get; set; }
        public long long_field { get; set; }
        public ulong ulong_field { get; set; }
        public nint nint_field { get; set; }
        public nuint nuint_field { get; set; }
        public float float_field { get; set; }
        public double double_field { get; set; }
        public decimal decimal_field { get; set; }
        public bool bool_field { get; set; }
        public char char_field { get; set; }
        public Season season_field { get; set; }
        public DateTime dateTime_field { get; set; }
        public ValueTuple<double, int> valuetuple_field { get; set; }
        public long? nulllong_field { get; set; }

        [NotNull]
        public string string_field { get; set; }

        public event AnotherDelegate event_field;

        public MessageDelegate delegate_field;

        public IEntity interface_field { get; set; }

        public T1ApiAddress class_field { get; set; }

        public Person record_field { get; set; }

        ///// <summary>
        /////  public、protected internal、protected、internal、private 或 private protected
        ///// </summary>
        private long privatelong_field { get; set; }

        protected long protectedlong_field { get; set; }

        protected internal long protectedinternallong_field { get; set; }

        internal long internallong_field { get; set; }

        protected long privateprotectedlong_field { get; set; }
    }

    public record class Person(string FirstName, string LastName);

    public delegate void MessageDelegate(string message);

    public delegate int AnotherDelegate(Season m, long num);

    public class T1ApiAddress : IEntity
    {
        /// <summary>
        /// API地址
        /// </summary>
        [NotNull]
        public string api_address { get; set; }

        /// <summary>
        /// API编码
        /// </summary>
        [NotNull]
        public string api_code { get; set; }

        /// <summary>
        /// API地址名称
        /// </summary>
        [NotNull]
        public string api_name { get; set; }

        public DateTime create_on { get; set; }
    }

    public interface IEntity
    {
        DateTime create_on { get; set; }
    }

    public enum Season
    {
        Spring,
        Summer,
        Autumn,
        Winter
    }
}