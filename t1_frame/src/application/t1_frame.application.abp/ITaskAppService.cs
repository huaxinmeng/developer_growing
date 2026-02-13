using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using t1_frame.entityframeworkcore.abp;
using t1_frame.response.abp;
using t1_frame.response.abp.Dto;
using Volo.Abp.Application.Services;
using Volo.Abp.Uow;

namespace t1_frame.application.abp
{
    public interface ITaskAppService : IApplicationService//, IUnitOfWorkEnabled
    {
        //[UnitOfWork]
        Task<string> GetTaskName(int id);

        Task<MessageDto?> AddMessage(MessageInput input);

        Task<string?> GetMessage(string id);
    }
}
