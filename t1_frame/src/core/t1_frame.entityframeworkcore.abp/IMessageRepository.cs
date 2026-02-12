using Microsoft.AspNetCore.Identity;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories.MongoDB;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.MongoDB;

namespace t1_frame.entityframeworkcore.abp
{
    public interface IMessageRepository : IMongoDbRepository<Message, ObjectId>
    {
        Task<Message?> GetLastEntity();
    }

    public class MessageRepository : SimpleMessageRepositoryBase<Message, ObjectId>, IMessageRepository
    {
        public MessageRepository(IMongoDbContextProvider<T1FrameMongoDbContext> dbContextProvider)
           : base(dbContextProvider)
        {
        }

        public async Task<Message?> GetLastEntity()
        {
            var query = await GetListAsync();
            return query.OrderBy(t => t.CreatedAt).LastOrDefault();
        }
    }

    public class SimpleMessageRepositoryBase<TEntity, TPrimaryKey> : MongoDbRepository<T1FrameMongoDbContext, TEntity, TPrimaryKey> where TEntity : class, IEntity<TPrimaryKey>
    {
        public SimpleMessageRepositoryBase(
            IMongoDbContextProvider<T1FrameMongoDbContext> dbContextProvider)
            : base(dbContextProvider)
        {
        }
    }
}
