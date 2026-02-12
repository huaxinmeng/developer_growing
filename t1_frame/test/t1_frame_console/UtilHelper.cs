namespace t1_frame_console
{
    public static class UtilHelper
    {
        /// <summary>
        /// 检查序列是否为 null 或者为空。
        /// </summary>
        /// <typeparam name="T">序列元素类型</typeparam>
        /// <param name="source">要检测的序列</param>
        /// <returns>是否为 null 或者空序列</returns>
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> source)
        {
            if (source == null)
                return true;

            return !source.Any();
        }
    }

    public sealed class DynamicEqualityComparer<T> : IEqualityComparer<T> where T : class
    {
        private readonly Func<T, T, bool> _func;

        public DynamicEqualityComparer(Func<T, T, bool> func)
        {
            _func = func;
        }

        public bool Equals(T x, T y) => _func(x, y);

        public int GetHashCode(T obj) => 0;
    }
}