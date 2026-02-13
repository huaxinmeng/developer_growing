using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using t1_frame.entityframeworkcore.abp;
using t1_frame.response.abp.Dto;
using Volo.Abp.AutoMapper;

namespace t1_frame.response.abp.Mapping
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<MessageInput, Message>();
            CreateMap<MessageReplyInput, MessageReply>();
            CreateMap<Message, MessageDto>();
            //.IgnoreAuditedObjectProperties();
        }
    }
}
