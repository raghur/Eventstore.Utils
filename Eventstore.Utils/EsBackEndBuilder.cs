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
        public MessageSender EventPublisher { get; set; }
        public IRepository Repository { get; set; }
        public IStoreEvents EventStore { get; set; }
        public IDocumentStore DocumentStore { get; set; }
    }
    public class EsBackEndBuilder
    {
        private readonly string prefix;
        private Conventions conventions;
        private string eventStoreDbConn;
        private Type[] typesToStream;
        private string storageAccountConnectionString;
        protected bool ConfigureDocStore;
        private bool configureCommandSender;


        public EsBackEndBuilder(string prefix)
        {
            this.prefix = prefix;
            this.conventions = new Conventions(prefix);
        }

        [Obsolete("Use ctor with prefix and the WithEventStore method")]
        public EsBackEndBuilder(string prefix, string eventStoreDbConn)
        {
            this.prefix = prefix;
            this.eventStoreDbConn = eventStoreDbConn;
        }

        public EsBackEndBuilder WithEventStore(string eventStoreDbConn)
        {
            this.eventStoreDbConn = eventStoreDbConn;
            return this;
        }

        
        public EsBackEndBuilder WithCommandSender(string storageConn, params Type[] types)
        {
            this.configureCommandSender = true;
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

       


        public EsBackEnd Build()
        {
            var backEnd = new EsBackEnd();
            

            if (this.configureCommandSender)
            {
                var streamer = Contracts.CreateStreamer(typesToStream);
                var config = AzureStorage.CreateConfig(storageAccountConnectionString);
                backEnd.CommandSender = new MessageSender(
                                                streamer,
                                                config.CreateQueueWriter(conventions.DefaultRouterQueue));
                backEnd.EventPublisher = new MessageSender(streamer, config.CreateQueueWriter(conventions.EventProcessingQueue));
            }
            
            if (this.ConfigureDocStore)
            {
                var config = AzureStorage.CreateConfig(storageAccountConnectionString);
                var viewStrategy = new ViewStrategy(new Conventions(prefix));
                backEnd.DocumentStore = config.CreateDocumentStore(viewStrategy);
            }
            if (!string.IsNullOrEmpty(this.eventStoreDbConn))
            {
                backEnd.EventStore = Wireup.Init()
                                   .LogToOutputWindow()
                                   .UsingSqlPersistence(new PassthroughConnectionFactory(eventStoreDbConn))
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