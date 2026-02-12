using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace t1_frame.entityframeworkcore.abp
{
    public class Message : MessageReply
    {
        /// <summary>
        /// 标记
        /// </summary>
        public string? Tag { get; set; }

        /// <summary>
        /// 别名
        /// </summary>
        public string? Alias { get; set; }

        public List<MessageReply>? Reply { get; set; }
    }
}
