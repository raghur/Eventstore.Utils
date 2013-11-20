using System;
using CQRS.Engine;
using CommonDomain.Core;
using CommonDomain.Persistence;
using CommonDomain.Persistence.EventStore;
using EventStore;
using EventStore.Persistence.SqlPersistence.SqlDialects;
using Lokad.Cqrs;
using Lokad.Cqrs.AtomicStorage;

namespace Eventstore.Utils
{
    public class EsBackEnd
    {
        public MessageSender CommandSender { get; set; }
        public IRepository Repository { get; set; }
        public IStoreEvents EventStore { get; set; }
        public IDocumentStore DocumentStore { get; set; }
    }
    public class EsBackEndBuilder
    {
        private string prefix; 
        private string eventStoreDBConn;
        private Type[] typesToStream;
        private string storageAccountConnectionString;
        public EsBackEndBuilder()
        {
            
        }
        public EsBackEndBuilder(string prefix, string eventStoreDbConn)
        {
            this.prefix = prefix;
            eventStoreDBConn = eventStoreDbConn;
        }

        public EsBackEndBuilder WithEventStore(string prefix, string eventStoreDbConn)
        {
            this.prefix = prefix;
            eventStoreDBConn = eventStoreDbConn;
            return this;
        }

        
        public EsBackEndBuilder WithCommandSender(string storageConn, params Type[] types)
        {
            this.ConfigureCommandSender = true;
            this.typesToStream = types;
            this.storageAccountConnectionString = storageConn;
            return this;
        }
        
        public EsBackEndBuilder WithDocumentStore(string storageConn)
        {
            this.ConfigureDocStore = true;
            this.storageAccountConnectionString = storageConn;
            return this;
        }

        protected bool ConfigureDocStore;
        private bool ConfigureCommandSender;


        public EsBackEnd Build()
        {
            var backEnd = new EsBackEnd();
            

            if (this.ConfigureCommandSender)
            {
                var Streamer = Contracts.CreateStreamer(typesToStream);
                var config = AzureStorage.CreateConfig(storageAccountConnectionString);
                backEnd.CommandSender = new MessageSender(
                                                Streamer,
                                                config.CreateQueueWriter(prefix.DefaultRouterQueue()));
            }
            
            if (this.ConfigureDocStore)
            {
                var config = AzureStorage.CreateConfig(storageAccountConnectionString);
                var viewStrategy = new ViewStrategy();
                backEnd.DocumentStore = config.CreateDocumentStore(viewStrategy);
            }
            if (!string.IsNullOrEmpty(this.eventStoreDBConn))
            {
                backEnd.EventStore = Wireup.Init()
                                   .LogToOutputWindow()
                                   .UsingSqlPersistence(new PassthroughConnectionFactory(eventStoreDBConn))
                                   .WithDialect(new MsSqlDialect())
                                   .EnlistInAmbientTransaction()
                                   .InitializeStorageEngine()
                                   .UsingServiceStackJsonSerialization()
                                   .Build();
                backEnd.Repository = new EventStoreRepository(backEnd.EventStore, new AggregateFactory(), new ConflictDetector());    
            }
            
            return backEnd;
        }




        
    }
}