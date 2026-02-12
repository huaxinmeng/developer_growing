using Abp.AspNetCore.Mvc.Controllers;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.Transactions;
using t1_frame.condition;
using t1_frame.domain;
using t1_frame.entityframeworkcore;
using t1_frame.entityframeworkcore.migrations;

namespace t1_frame.webapi.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class HomeController : AbpController
    {
        //private readonly T1FrameDbContext _t1FrameDbContext;
        private readonly IDbContextProvider<T1FrameDbContext> _dbContextProvider;
        private readonly IRepository<T1ApiBase, long> _repository;
        private readonly IRepository<Student, int> _strepository;
        public HomeController(IDbContextProvider<T1FrameDbContext> dbContextProvider,
            IRepository<T1ApiBase, long> repository, IRepository<Student, int> strepository)
        {
            _dbContextProvider = dbContextProvider;
            Console.WriteLine($"{this.GetHashCode()}");

            _repository = repository;
            _strepository = strepository;
        }

        [HttpGet]
        public IResult GetStr(string name)
        {
            var _t1FrameDbContext = _dbContextProvider.GetDbContext();
            _t1FrameDbContext.T1ApiBase.Add(new domain.T1ApiBase() { sys_code = "loc" });
            _t1FrameDbContext.T1ApiBase.Add(new domain.T1ApiBase() { sys_code = "loc1" });
            _t1FrameDbContext.SaveChanges();
            return TypedResults.Ok(_t1FrameDbContext.T1ApiBase.ToList());
        }

        [HttpPost]
        public string Test(TestCondition condition)
        {
            return "369";
        }

        [HttpGet]
        public IActionResult GetStr1(string name)
        {
            var _t1FrameDbContext = _dbContextProvider.GetDbContext();
            _t1FrameDbContext.T1ApiBase.Add(new domain.T1ApiBase() { sys_code = "loc" });
            _t1FrameDbContext.T1ApiBase.Add(new domain.T1ApiBase() { sys_code = "loc1" });
            _t1FrameDbContext.SaveChanges();
            return Ok(_t1FrameDbContext.T1ApiBase.ToList());
        }

        [HttpGet]
        // [UnitOfWork(isTransactional: false)]
        public async Task<bool> TestStudent()
        {
            try
            {
                // await _repository.InsertAsync(new domain.T1ApiBase() { sys_code = "loc" });

                var uowManager = IocManager.Instance.Resolve<IUnitOfWorkManager>();

                // 第一个 UoW - T1ApiBase
                using (var uow1 = uowManager.Begin(TransactionScopeOption.RequiresNew))
                {
                    await _repository.InsertAsync(new T1ApiBase { sys_code = "loc" });
                    await uow1.CompleteAsync();  // 立即提交
                }

                var count1 = await _repository.CountAsync();
                //if (count > 14)
                //{
                //    //throw new IndexOutOfRangeException();
                //}

                await _repository.InsertAsync(new domain.T1ApiBase() { sys_code = "loc1" });

                await _strepository.InsertAsync(new Student { Name = "小红", Sno = "2" });


                // 重要：手动保存更改
                await CurrentUnitOfWork.SaveChangesAsync();

                var dataList = (await _repository.GetAllAsync()).ToList();

                var count = await _repository.CountAsync();
                if (count > 14)
                {
                    throw new IndexOutOfRangeException();
                }

                return true;

            }
            catch (Exception ex)
            {
                throw;
                return false;
            }
        }
    }
}
