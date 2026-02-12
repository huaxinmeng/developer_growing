using Abp.Application.Services;
using Abp.Dapper.Repositories;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.EntityFrameworkCore;
using Abp.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using t1_frame.domain;
using t1_frame.entityframeworkcore;

namespace t1_frame.application
{
    // [DontWrapResult]
    public class TaskAppService : ApplicationService, ITaskAppService
    {
        private readonly IDbContextProvider<T1FrameDbContext> _dbContextProvider;
        private readonly IRepository<T1ApiBase, long> _repository;
        private readonly ITaskRepository _taskRepository;
        private readonly IDapperRepository<T1ApiBase, long> _drepository;

        public TaskAppService(IDbContextProvider<T1FrameDbContext> dbContextProvider, IRepository<T1ApiBase, long> repository, ITaskRepository taskRepository, IDapperRepository<T1ApiBase, long> drepository)
        {
            _dbContextProvider = dbContextProvider;
            _repository = repository;
            _taskRepository = taskRepository;
            _drepository = drepository;
        }

        //[UnitOfWork(false, IsDisabled = true)]
        public async Task<string> GetTaskName(int id)
        {
            try
            {
                //var dbContext = await _dbContextProvider.GetDbContextAsync();
                ////using (var dbContext = await _dbContextProvider.GetDbContextAsync())
                //{
                //    dbContext.T1ApiBase.Add(new domain.T1ApiBase() { sys_code = "loc" });
                //    dbContext.T1ApiBase.Add(new domain.T1ApiBase() { sys_code = "loc1" });
                //    dbContext.SaveChanges();
                //    var dt = dbContext.T1ApiBase.ToList();
                //    return JsonConvert.SerializeObject(dt);
                //}

                await _repository.InsertAsync(new domain.T1ApiBase() { sys_code = "loc" });
                await _repository.InsertAsync(new domain.T1ApiBase() { sys_code = "loc1" });

                // 重要：手动保存更改
                await CurrentUnitOfWork.SaveChangesAsync();
                var dataList = (await _repository.GetAllAsync()).ToList();

                var rty = _taskRepository.GetLastEntity();
                var tOmne = _drepository.Query("select * from t1_api_base order by 1 desc limit 1");
                return JsonConvert.SerializeObject(tOmne);
                //return new JsonResult(dataList);
            }
            catch (Exception ex)
            {

            }

            return null;// "ths is a test";
        }
    }
}
