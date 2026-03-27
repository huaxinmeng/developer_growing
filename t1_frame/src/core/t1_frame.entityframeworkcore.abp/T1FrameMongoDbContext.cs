using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Data;
using Volo.Abp.MongoDB;
using Volo.Abp.MongoDB.DistributedEvents;

namespace t1_frame.entityframeworkcore.abp
{
    [ConnectionStringName("mongodb")]
    public class T1FrameMongoDbContext : AbpMongoDbContext, IHasEventOutbox, IHasEventInbox
    {
        public IMongoCollection<Message> Messages => Collection<Message>();
        public IMongoCollection<OutgoingEventRecord> OutgoingEvents => Collection<OutgoingEventRecord>();
        public IMongoCollection<IncomingEventRecord> IncomingEvents => Collection<IncomingEventRecord>();

        protected override void CreateModel(IMongoModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Message>(b =>
            {
                b.CollectionName = "t1_message";
                b.BsonMap.AutoMap();
                b.BsonMap.SetIgnoreExtraElements(true);
            });

            modelBuilder.ConfigureEventOutbox();
            modelBuilder.ConfigureEventInbox();
            base.CreateModel(modelBuilder);
        }
    }
}
