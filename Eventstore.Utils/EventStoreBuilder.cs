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
    public class EventStoreBuilder
    {
        public MessageSender CommandSender { get; set; }
        public IRepository Repository { get; set; }
        public IStoreEvents EventStore { get; set; }

        public EventStoreBuilder(string prefix, string storageAccountConnectionString, string eventStoreDBConn, params Type[] typesToStream)
        {
            var config = AzureStorage.CreateConfig(storageAccountConnectionString);
            var Streamer = Contracts.CreateStreamer(typesToStream);
            CommandSender = new MessageSender(
                Streamer,
                config.CreateQueueWriter(prefix.DefaultRouterQueue()));


            EventStore = Wireup.Init()
                               .LogToOutputWindow()
                               .UsingSqlPersistence(new AzureConfigurationConnectionFactory(prefix + "EventStore", eventStoreDBConn))
                               .WithDialect(new MsSqlDialect())
                               .EnlistInAmbientTransaction()
                               .InitializeStorageEngine()
                               .UsingServiceStackJsonSerialization()
                               .Build();
            IRepository repository = new EventStoreRepository(EventStore, new AggregateFactory(), new ConflictDetector());

        }


        
    }
}