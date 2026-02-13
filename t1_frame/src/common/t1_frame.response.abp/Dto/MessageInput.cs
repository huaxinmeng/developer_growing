using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace t1_frame.response.abp
{
    public class MessageInput
    {
        /// <summary>
        /// 标记
        /// </summary>
        public string Tag { get; set; }

        public List<MessageReplyInput> Reply { get; set; }
    }

    public class MessageReplyInput
    {
        public string UserId { get; set; }

        public string Name { get; set; }

        public string Avatar { get; set; }

        public string Content { get; set; }
    }
}
