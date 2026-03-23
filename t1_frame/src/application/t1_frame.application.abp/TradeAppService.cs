using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using t1_frame.core.abp;
using t1_frame.entityframeworkcore.abp;
using t1_frame.response.abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Caching;
using Volo.Abp.Domain.Repositories;

namespace t1_frame.application.abp
{
    public class TradeAppService : ApplicationService, ITradeAppService
    {
        private readonly IRepository<T1User, long> _userRepository;
        private readonly IRepository<T1TradeLog, long> _logRepository;
        private readonly IRepository<T1UserAccount, long> _accountRepository;
        private readonly IDistributedCache<string> _cache;
        public TradeAppService(IRepository<T1User, long> userRepository, 
            IRepository<T1TradeLog, long> logRepository, 
            IRepository<T1UserAccount, long> accountRepository, 
            IDistributedCache<string> cache)
        {
            _userRepository = userRepository;
            _logRepository = logRepository;
            _accountRepository = accountRepository;
            _cache = cache;
        }

        public async virtual Task<bool> Deduct(DeductInput input)
        {
            var user =  await _userRepository.FirstOrDefaultAsync(t => t.user_code == input.user_code);

            if (user == null)
            {
                throw new Exception($"用户{input.user_code}不存在...");
            }

            var account = await _accountRepository.FirstOrDefaultAsync(t => t.user_id == user.Id);
            if(account == null)
            {
                throw new Exception($"用户{user.user_name}未充值...");
            }
            else if(account.amount < input.cost)
            {
                throw new Exception($"用户{user.user_name}余额不足{account.amount}...");
            }

            account.amount -= input.cost;
            var delayVal = RandomHelper.Instance.GetRandomCtl().Next(0, 10);
            await DelayHelper.DoWorkWithTimeoutAsync(delay: TimeSpan.FromSeconds(delayVal), timeout: TimeSpan.FromSeconds(3));

            await _logRepository.InsertAsync(new T1TradeLog
            {
                user_id = user.Id,
                source = "Deduct",
                description = $"{user.user_name} 扣减 {input.cost}"
            });

            return true;
        }



    }
}
