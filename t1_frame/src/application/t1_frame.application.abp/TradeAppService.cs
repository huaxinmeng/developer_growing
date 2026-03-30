using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using MySqlConnector;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using t1_frame.core.abp;
using t1_frame.entityframeworkcore.abp;
using t1_frame.response.abp;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Caching;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Uow;
using static MongoDB.Driver.WriteConcern;
using static Pipelines.Sockets.Unofficial.Threading.MutexSlim;

namespace t1_frame.application.abp
{
    public class TradeAppService : ApplicationService, ITradeAppService
    {
        private readonly IRepository<T1User, long> _userRepository;
        private readonly IRepository<T1TradeLog, long> _logRepository;
        private readonly IRepository<T1UserAccount, long> _accountRepository;
        private readonly IDistributedCache<string> _cache;
        private readonly IDatabase _db;
        private readonly IDistributedEventBus _distributedEventBus;
        private readonly IRepository<T1GoodsStock, long> _stockRepository;
        public TradeAppService(IRepository<T1User, long> userRepository, 
            IRepository<T1TradeLog, long> logRepository, 
            IRepository<T1UserAccount, long> accountRepository, 
            IDistributedCache<string> cache,
            IConnectionMultiplexer redis,
            IDistributedEventBus distributedEventBus,
            IRepository<T1GoodsStock, long> stockRepository)
        {
            _userRepository = userRepository;
            _logRepository = logRepository;
            _accountRepository = accountRepository;
            _cache = cache;
            _db = redis.GetDatabase();
            _distributedEventBus = distributedEventBus;
            _stockRepository = stockRepository;
        }

        // [HttpPost]
        public async virtual Task<bool> Deduct(DeductInput input)
        {
            var user =  await _userRepository.FirstOrDefaultAsync(t => t.user_code == input.user_code);

            if (user == null)
            {
                throw new AbpException($"用户{input.user_code}不存在...");
            }

            var account = await _accountRepository.FirstOrDefaultAsync(t => t.user_id == user.Id);
            // 排他锁很影响连接池的使用
            // var account = await (await _accountRepository.GetDbSetAsync()).FromSqlInterpolated($"SELECT * FROM t1_user_account WHERE user_id = {user.Id} FOR UPDATE").FirstOrDefaultAsync();
            if (account == null)
            {
                throw new AbpException($"用户{user.user_name}未充值...");
            }
            else if (account.amount < input.cost)
            {
                throw new AbpException($"用户{user.user_name}余额不足{account.amount}...");
            }

            // account.amount -= input.cost;

            var sql = @"
                        UPDATE t1_user_account 
                        SET amount = amount - @cost, 
                            version = version + 1 
                        WHERE user_id = @userId 
                          AND version = @version";

            var parameters = new[]
            {
                new MySqlParameter("@cost", input.cost),
                new MySqlParameter("@userId", user.Id),
                new MySqlParameter("@version", account.version)
            };

            var rowsAffected = await (await _accountRepository
                                .GetDbContextAsync()).Database
                                .ExecuteSqlRawAsync(sql, parameters);
            if (rowsAffected == 0)
            {
                throw new AbpException("并发冲突，请重试");
            }

            // 数据库的排他锁
            /* （取巧方案, Mysql）
            FormattableString sql = $@"
                                        UPDATE t1_user_account 
                                        SET amount = amount - {input.cost}
                                        WHERE user_id = {user.Id} AND amount >= {input.cost}";

            var rowsAffected = await (await _accountRepository
                .GetDbContextAsync()).Database
                .ExecuteSqlInterpolatedAsync(sql);

            if (rowsAffected == 0)
            {
                // 可能是余额不足或记录不存在
                throw new Exception("扣减失败，余额不足或用户不存在");
            }
            */

            var delayVal = core.abp.RandomHelper.Instance.GetRandomCtl().Next(0, 10);
            await DelayHelper.DoWorkWithTimeoutAsync(delay: TimeSpan.FromSeconds(delayVal), timeout: TimeSpan.FromSeconds(20));

            await _logRepository.InsertAsync(new T1TradeLog
            {
                user_id = user.Id,
                source = "Deduct",
                description = $"{user.user_name} 扣减 {input.cost}"
            });

            return true;
        }

        public async virtual Task<bool> DeductWithLua(DeductInput input)
        {
            var user = await _userRepository.FirstOrDefaultAsync(t => t.user_code == input.user_code);

            if (user == null)
            {
                throw new AbpException($"用户{input.user_code}不存在...");
            }

            var account = await _accountRepository.FirstOrDefaultAsync(t => t.user_id == user.Id);
            if (account == null)
            {
                throw new AbpException($"用户{user.user_name}未充值...");
            }
            else if (account.amount < input.cost)
            {
                throw new AbpException($"用户{user.user_name}余额不足{account.amount}...");
            }

            var balanceKey = $"user:{input.user_code}:balance";
            var lockKey = $"lock:{input.user_code}";
            var lockValue = Guid.NewGuid().ToString("N");

            //var exists = await _db.KeyExistsAsync(balanceKey);
            //var value = await _db.StringGetAsync(balanceKey);

            // Lua 脚本：原子检查余额并扣减
            //    var deductScript = @"
            //    local balance = redis.call('GET', KEYS[1])
            //    if not balance then
            //        return {-1, '用户不存在'}
            //    end
            //    if tonumber(balance) < tonumber(ARGV[1]) then
            //        return {-2, '余额不足'}
            //    end
            //    local newBalance = redis.call('INCRBYFLOAT', KEYS[1], ARGV[1])
            //    return {1, newBalance}
            //";

            var deductScript = @"
                        local balanceKey = KEYS[1]
                        local lockKey = KEYS[2]        -- 新增：操作锁
                        local cost = tonumber(ARGV[1])
                        local lockToken = ARGV[2]      -- 新增：锁标识
                        local lockExpire = tonumber(ARGV[3])  -- 新增：锁过期时间（秒）

                        -- 防重复提交检查
                        if redis.call('EXISTS', lockKey) == 1 then
                            return {-3, '操作处理中'}
                        end

                        local balance = redis.call('GET', balanceKey)
                        if not balance then
                            return {-1, '用户不存在'}
                        end

                        if tonumber(balance) < cost then
                            return {-2, '余额不足'}
                        end

                        -- 扣减并设置操作锁
                        local newBalance = redis.call('INCRBYFLOAT', balanceKey, cost)
                        redis.call('SETEX', lockKey, lockExpire, lockToken)

                        return {1, newBalance}";

            try
            {

                // 先尝试原子扣减
                var result = (RedisResult[])await _db.ScriptEvaluateAsync(deductScript,
                    new RedisKey[] { balanceKey, lockKey },
                    new RedisValue[] { -Convert.ToDouble(input.cost), lockValue, 30 }
                );

                var code = (long)result[0];

                if (code == -1) throw new AbpException("用户不存在");
                if (code == -2) throw new AbpException("余额不足");
                if (code == -3) throw new AbpException("操作处理中");

                var newBalance = (double)result[1];

                account.amount = (decimal)newBalance;

                var delayVal = core.abp.RandomHelper.Instance.GetRandomCtl().Next(0, 10);
                await DelayHelper.DoWorkWithTimeoutAsync(delay: TimeSpan.FromSeconds(delayVal), timeout: TimeSpan.FromSeconds(20));

                await _logRepository.InsertAsync(new T1TradeLog
                {
                    user_id = user.Id,
                    source = "Deduct",
                    description = $"{user.user_name} 扣减 {input.cost}"
                });

                return true;
            }
            //catch (Exception ex)
            //{
            //    throw;
            //}
            finally
            {
                var lua = @"
                            if redis.call('GET', KEYS[1]) == ARGV[1] then
                                return redis.call('DEL', KEYS[1])
                            else
                                return 0
                            end
                        ";
                await _db.ScriptEvaluateAsync(lua, new RedisKey[] { lockKey }, new RedisValue[] { lockValue });
            }
        }

        public async virtual Task<bool> DeductWithRedis(DeductInput input)
        {
            var lockKey = $"lock:deduct:{input.user_code}";
            var lockValue = Guid.NewGuid().ToString("N");

            // 1. 获取分布式锁（30秒过期，防止死锁）
            var lockAcquired = await _cache.GetOrAddAsync(
                lockKey,
                async () => await Task.FromResult(lockValue),
                () => new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(3600) }
            );

            if (lockAcquired != lockValue)
                throw new AbpException("操作太频繁，请稍后重试");

            try
            {
                // 2. 使用乐观锁/悲观锁执行扣减（同方案1）
                return await Deduct(input);
            }
            finally
            {
                // 3. 释放锁
                await _cache.RemoveAsync(lockKey);
            }
        }

        // [UnitOfWork(isTransactional: false)]
        public virtual async Task<bool> EventBusTest(GoodsRaceEto input)
        {
            T1User user = null;
            //var user = await _userRepository.FirstOrDefaultAsync(t => t.user_code == input.user_code);
            //if (user == null)
            //{
            //    throw new AbpException($"用户{input.user_code}不存在...");
            //}

            //var product = await _stockRepository.FirstOrDefaultAsync(t => t.goods_name == input.goods_name);
            //if (product == null)
            //{
            //    throw new AbpException($"商品{input.goods_name}未上架...");
            //}
            //else if (product.goods_stock < input.goods_stock)
            //{
            //    throw new AbpException($"商品{input.goods_name}库存不足{product.goods_stock}...");
            //}

            var balanceKey = $"product:{input.goods_name}:stock";
            var lockKey = $"lock:{input.user_code}:{input.goods_name}";
            var lockValue = Guid.NewGuid().ToString("N");

            var deductScript = @"
                        -- 正确的原子操作：先加锁，再检查/扣减
                        local balanceKey = KEYS[1]
                        local lockKey = KEYS[2]
                        local cost = tonumber(ARGV[1])
                        local lockToken = ARGV[2]
                        local lockExpire = tonumber(ARGV[3])

                        -- 1. 先尝试获取锁（SET NX EX）
                        local acquired = redis.call('SET', lockKey, lockToken, 'NX', 'EX', lockExpire)
                        if not acquired then
                            return {-3, '操作处理中'}
                        end

                        -- 2. 获取当前库存
                        local balance = redis.call('GET', balanceKey)
                        if not balance then
                            redis.call('DEL', lockKey)  -- 释放锁
                            return {-1, '商品不存在'}
                        end

                        -- 3. 检查并扣减
                        if tonumber(balance) < cost then
                            redis.call('DEL', lockKey)  -- 释放锁
                            return {-2, '库存不足'}
                        end

                        -- 4. 执行扣减
                        local newBalance = redis.call('DECRBY', balanceKey, cost)

                        -- 5. 保留锁（让 finally 块释放），或立即释放
                        -- 注意：这里不释放锁，让业务逻辑完成后释放

                        return {1, newBalance}";

            try
            {
                // 先尝试原子扣减
                var result = (RedisResult[])await _db.ScriptEvaluateAsync(deductScript,
                    new RedisKey[] { balanceKey, lockKey },
                    new RedisValue[] { input.goods_stock, lockValue, 30 }
                );

                var code = (long)result[0];

                if (code == -1) throw new AbpException("商品不存在");
                if (code == -2) throw new AbpException("库存不足");
                if (code == -3) throw new AbpException("操作处理中");

                var newBalance = (double)result[1];

                // account.amount = (decimal)newBalance;

                //var delayVal = core.abp.RandomHelper.Instance.GetRandomCtl().Next(0, 10);
                //await DelayHelper.DoWorkWithTimeoutAsync(delay: TimeSpan.FromSeconds(delayVal), timeout: TimeSpan.FromSeconds(20));
                await _distributedEventBus.PublishAsync(
                        new GoodsRaceActEto(input, lockValue)
                        );

                //await _logRepository.InsertAsync(new T1TradeLog
                //{
                //    user_id = user?.Id ?? 0,
                //    source = "Stock",
                //    description = $"{user?.user_name ?? input.user_code} 扣减 {input.goods_stock}"
                //});

                return true;
            }
            //catch (Exception ex)
            //{
            //    throw;
            //}
            finally
            {
                var lua = @"
                            if redis.call('GET', KEYS[1]) == ARGV[1] then
                                return redis.call('DEL', KEYS[1])
                            else
                                return 0
                            end
                        ";
                await _db.ScriptEvaluateAsync(lua, new RedisKey[] { lockKey }, new RedisValue[] { lockValue });
            }
        }
    }
}
