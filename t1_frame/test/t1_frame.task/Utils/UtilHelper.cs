using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace t1_frame.task.Utils
{
    internal static class UtilHelper
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

        public static T CastAnonymous<T>(this object anonymous, T anonymousType)
        {
            return (T)((object)anonymous);
        }
    }
}
