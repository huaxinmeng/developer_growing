using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using t1_frame.response.abp;
using Volo.Abp.Application.Services;

namespace t1_frame.application.abp
{
    public interface IUserAppService : IApplicationService
    {
        Task<bool> AddUser(UserInput input);

        Task<bool> Deposit(UserAcountInput input);
    }
}
