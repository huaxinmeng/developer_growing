using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using t1_frame.entityframeworkcore.abp;
using t1_frame.response.abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.ObjectMapping;

namespace t1_frame.application.abp
{
    public class UserAppService : ApplicationService, IUserAppService
    {
        private readonly IRepository<T1User, long> _repository;
        private readonly IRepository<T1UserAccount, long> _accountRepository;
        public UserAppService(IRepository<T1User, long> repository,
            IRepository<T1UserAccount, long> accountRepository)
        {
            _repository = repository;
            _accountRepository = accountRepository;
        }

        public async virtual Task<bool> AddUser(UserInput input)
        {
            var result = ObjectMapper.Map<UserInput, T1User>(input);
            await _repository.InsertAsync(result);

            return true;
        }

        public async virtual Task<bool> Deposit(UserAcountInput input)
        {
            var user = await _repository.FirstOrDefaultAsync(t => t.user_code == input.user_code);

            if (user == null)
            {
                throw new Exception($"用户{input.user_code}不存在...");
            }

            var account = await _accountRepository.FirstOrDefaultAsync(t => t.user_id == user.Id);
            if(account == null)
            {
                var result = ObjectMapper.Map<UserAcountInput, T1UserAccount>(input);
                await _accountRepository.InsertAsync(result);
            }
            else
            {
                account.amount += input.amount;
            }
            
            return true;
        }
    }
}
