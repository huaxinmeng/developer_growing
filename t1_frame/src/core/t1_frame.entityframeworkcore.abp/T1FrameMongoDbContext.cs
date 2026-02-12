using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Data;
using Volo.Abp.MongoDB;

namespace t1_frame.entityframeworkcore.abp
{
    [ConnectionStringName("mongodb")]
    public class T1FrameMongoDbContext : AbpMongoDbContext
    {
        public IMongoCollection<Message> Messages => Collection<Message>();

        protected override void CreateModel(IMongoModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Message>(b =>
            {
                b.CollectionName = "t1_message";
                b.BsonMap.AutoMap();
                b.BsonMap.SetIgnoreExtraElements(true);
            });

            base.CreateModel(modelBuilder);
        }
    }
}
