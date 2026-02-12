using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Domain.Entities;

namespace t1_frame.entityframeworkcore.abp
{
    public class MessageReply : Entity<ObjectId>
    {
        public MessageReply()
        {
            Id = ObjectId.GenerateNewId();
            CreatedAt = DateTime.Now;
        }

        public string UserId { get; set; }

        public string Name { get; set; }

        public string Avatar { get; set; }

        public string Content { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
