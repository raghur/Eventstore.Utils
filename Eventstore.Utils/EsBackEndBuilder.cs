using System;
using CQRS.Engine;
using CommonDomain.Core;
using CommonDomain.Persistence;
using CommonDomain.Persistence.EventStore;
using EventStore;
using EventStore.Persistence.SqlPersistence.SqlDialects;
using Lokad.Cqrs;

namespace Eventstore.Utils
{
    public class EsBackEnd
    {
        public MessageSender CommandSender { get; set; }
        public IRepository Repository { get; set; }
        public IStoreEvents EventStore { get; set; }
    }
    public class EsBackEndBuilder
    {
        private string prefix; 
        private string eventStoreDBConn;
        private Type[] typesToStream;
        private string storageAccountConnectionString;

        public EsBackEndBuilder(string prefix, string eventStoreDbConn)
        {
            this.prefix = prefix;
            eventStoreDBConn = eventStoreDbConn;
        }

        public EsBackEndBuilder WithCommandSender(string storageConn, params Type[] types)
        {
            this.typesToStream = types;
            this.storageAccountConnectionString = storageConn;
            return this;
        }
        
        
        public EsBackEnd Build()
        {
            var backEnd = new EsBackEnd();
            

            if (!string.IsNullOrEmpty(this.storageAccountConnectionString))
            {
                var Streamer = Contracts.CreateStreamer(typesToStream);
                var config = AzureStorage.CreateConfig(storageAccountConnectionString);
                backEnd.CommandSender = new MessageSender(
                Streamer,
                config.CreateQueueWriter(prefix.DefaultRouterQueue()));
            }
            
            backEnd.EventStore = Wireup.Init()
                               .LogToOutputWindow()
                               .UsingSqlPersistence(new PassthroughConnectionFactory(eventStoreDBConn))
                               .WithDialect(new MsSqlDialect())
                               .EnlistInAmbientTransaction()
                               .InitializeStorageEngine()
                               .UsingServiceStackJsonSerialization()
                               .Build();
            backEnd.Repository = new EventStoreRepository(backEnd.EventStore, new AggregateFactory(), new ConflictDetector());
            return backEnd;
        }




        
    }
}