using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using t1_frame.entityframeworkcore.abp;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

namespace t1_frame.application.abp
{
    // [RemoteService]
    public class TaskAppService : ApplicationService, ITaskAppService
    {
        private readonly IRepository<T1ApiBase, long> _repository;
        private readonly ITaskRepository _taskRepository;
        private readonly IRepository<Student, int> _strepository;

        public TaskAppService(IRepository<T1ApiBase, long> repository, ITaskRepository taskRepository, IRepository<Student, int> strepository)
        {
            _repository = repository;
            _taskRepository = taskRepository;
            _strepository = strepository;
        }

        [UnitOfWork(true)]
        public async virtual Task<string> GetTaskName(int id)
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

         
                await _repository.InsertAsync(new T1ApiBase() { sys_code = "loc" });
                var count = await _repository.CountAsync();
                await _repository.InsertAsync(new T1ApiBase() { sys_code = "loc1" });

                await _strepository.InsertAsync(new Student { Name = "小全", Sno = "4" });

                // 重要：手动保存更改
                await CurrentUnitOfWork.SaveChangesAsync();
                var dataList = (await _repository.GetListAsync()).ToList();
                count = await _repository.CountAsync();
                //if (count > 8)
                //{
                //    throw new IndexOutOfRangeException();
                //}
                var rty = await _taskRepository.GetLastEntity();
                //var tOmne = _drepository.Query("select * from t1_api_base order by 1 desc limit 1");
                return JsonConvert.SerializeObject(rty);
                //return new JsonResult(dataList);


            }
            catch (Exception ex)
            {
                throw;
            }

            return null;// "ths is a test";
        }
    }
}
