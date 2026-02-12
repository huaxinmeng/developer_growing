using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Abp.EntityFrameworkCore;
using Abp.EntityFrameworkCore.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using t1_frame.domain;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace t1_frame.entityframeworkcore
{
    public interface ITaskRepository : IRepository<T1ApiBase, long>
    {
        T1ApiBase? GetLastEntity();
    }

    public class TaskRepository : SimpleTaskSystemRepositoryBase<T1ApiBase, long>, ITaskRepository
    {
        public TaskRepository(IDbContextProvider<T1FrameDbContext> dbContextProvider)
            : base(dbContextProvider)
        {
        }

        public T1ApiBase? GetLastEntity()
        {
            var query = GetAll();
            return query.OrderBy(t => t.Id).LastOrDefault();
        }
    }

    //所有仓储的基类
    public class SimpleTaskSystemRepositoryBase<TEntity, TPrimaryKey> : EfCoreRepositoryBase<T1FrameDbContext, TEntity, TPrimaryKey>
        where TEntity : class, IEntity<TPrimaryKey>
    {
        public SimpleTaskSystemRepositoryBase(IDbContextProvider<T1FrameDbContext> dbContextProvider)
            : base(dbContextProvider)
        {
        }

        //为所有的仓储添加一些公共的方法
    }

    ////Id为整数的实体的快捷方式
    //public class SimpleTaskSystemRepositoryBase<TEntity> : SimpleTaskSystemRepositoryBase<TEntity, int>
    //    where TEntity : class, IEntity<int>
    //{
    //    public SimpleTaskSystemRepositoryBase(IDbContextProvider<T1FrameDbContext> dbContextProvider)
    //        : base(dbContextProvider)
    //    {
    //    }

    //    //不要在这里添加任何方法，在上面的方法中添加（因为该方法继承了上面的方法）
    //}
}
