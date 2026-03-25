using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using t1_frame.entityframeworkcore.abp;
using t1_frame.response.abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Caching;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.ObjectMapping;

namespace t1_frame.application.abp
{
    public class UserAppService : ApplicationService, IUserAppService
    {
        private readonly IRepository<T1User, long> _repository;
        private readonly IRepository<T1UserAccount, long> _accountRepository;
        private readonly IDistributedCache<string> _cache;
        private readonly IDatabase _db;
        public UserAppService(IRepository<T1User, long> repository,
            IRepository<T1UserAccount, long> accountRepository,
            IDistributedCache<string> cache,
            IConnectionMultiplexer redis)
        {
            _repository = repository;
            _accountRepository = accountRepository;
            _cache = cache;
            _db = redis.GetDatabase();
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
                result.user_id = user.Id;
                await _accountRepository.InsertAsync(result);
            }
            else
            {
                account.amount += input.amount;
            }

            //await _cache.SetAsync($"user:{input.user_code}:balance", (account?.amount ?? input.amount).ToString());
            await _db.StringSetAsync($"user:{input.user_code}:balance", (account?.amount ?? input.amount).ToString());
            return true;
        }
    }
}
