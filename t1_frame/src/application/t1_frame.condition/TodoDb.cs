using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace t1_frame.condition
{
    public class TodoDb
    {
        public List<int> Todos {  get; set; } = Enumerable.Range(0,3).ToList();
    }
}
