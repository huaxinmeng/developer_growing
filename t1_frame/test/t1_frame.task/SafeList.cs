using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace t1_frame.task
{
    public class SafeList<T>
    {
        private readonly List<T> list = new List<T>();
        private readonly object lockObj = new object();

        public void Add(T item)
        {
            lock (lockObj)
            {
                list.Add(item);
            }
        }

        public bool Remove(T item)
        {
            lock (lockObj)
            {
                return list.Remove(item);
            }
        }

        public T[] ToArray()
        {
            lock (lockObj)
            {
                return list.ToArray();
            }
        }

        // 可以添加其他需要安全访问的方法

        public int Count
        {
            get
            {
                lock (lockObj)
                {
                    return list.Count;
                }
            }
        }
    }
}
