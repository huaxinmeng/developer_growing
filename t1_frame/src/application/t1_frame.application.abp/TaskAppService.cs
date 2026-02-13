using Microsoft.AspNetCore.Http.HttpResults;
using MongoDB.Bson;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using t1_frame.entityframeworkcore.abp;
using t1_frame.response.abp;
using t1_frame.response.abp.Dto;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Caching;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Repositories.MongoDB;
using Volo.Abp.Uow;

namespace t1_frame.application.abp
{
    // [RemoteService]
    public class TaskAppService : ApplicationService, ITaskAppService
    {
        private readonly IRepository<T1ApiBase, long> _repository;
        private readonly ITaskRepository _taskRepository;
        private readonly IRepository<Student, int> _strepository;
        // private readonly IMongoDbRepository<Message, ObjectId> _mongoDbRepository;
        private readonly IMessageRepository _messageRepository;
        private readonly IDistributedCache<string> _cache;

        public TaskAppService(IRepository<T1ApiBase, long> repository, 
            ITaskRepository taskRepository, 
            IRepository<Student, int> strepository,
            //IMongoDbRepository<Message, ObjectId> mongoDbRepository,
            IMessageRepository messageRepository,
            IDistributedCache<string> cache)
        {
            _repository = repository;
            _taskRepository = taskRepository;
            _strepository = strepository;
            // _mongoDbRepository = mongoDbRepository;
            _messageRepository = messageRepository;
            _cache = cache;
        }

        [UnitOfWork(isTransactional: false)]
        public async virtual Task<MessageDto?> AddMessage(MessageInput input)
        {
            var result = ObjectMapper.Map<MessageInput, Message>(input);
            await _messageRepository.InsertAsync(result);

            //await _messageRepository.InsertAsync(new Message
            //{
            //    Tag = input.Tag,
            //    Reply = input.Reply?.Select(t => new MessageReply
            //    {
            //        UserId = t.UserId,
            //        Name = t.Name,
            //        Avatar = t.Avatar,
            //        Content = t.Content,
            //        // CreatedAt = DateTime.Now
            //    }).ToList()
            //});

            var enity = await _messageRepository.GetLastEntity();
            if(enity != null)
            {
                await _cache.SetAsync("messageId", enity.Id.ToString());
            }

            return ObjectMapper.Map<Message, MessageDto>(enity);
        }

        public async Task<string?> GetMessage(string id)
        {
            return await _cache.GetAsync("messageId");
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
