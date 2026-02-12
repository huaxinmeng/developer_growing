using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace t1_frame.entityframeworkcore.abp
{
    public interface ITaskRepository : IRepository<T1ApiBase, long>
    {
        Task<T1ApiBase?> GetLastEntity();
    }

    public class TaskRepository : SimpleTaskSystemRepositoryBase<T1ApiBase, long>, ITaskRepository
    {
        public TaskRepository(IDbContextProvider<T1FrameAbpDbContext> dbContextProvider)
            : base(dbContextProvider)
        {
        }

        public async Task<T1ApiBase?> GetLastEntity()
        {
            var query = await GetListAsync();
            return query.OrderBy(t => t.Id).LastOrDefault();
        }
    }

    public class SimpleTaskSystemRepositoryBase<TEntity, TPrimaryKey> : EfCoreRepository<T1FrameAbpDbContext, TEntity, TPrimaryKey>
     where TEntity : class, IEntity<TPrimaryKey>
    {
        public SimpleTaskSystemRepositoryBase(IDbContextProvider<T1FrameAbpDbContext> dbContextProvider)
            : base(dbContextProvider)
        {
        }

        //为所有的仓储添加一些公共的方法
    }
}
