﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using CQRS.Engine;
using CommonDomain.Core;
using CommonDomain.Persistence;
using CommonDomain.Persistence.EventStore;
using EventStore;
using EventStore.Persistence;
using EventStore.Persistence.SqlPersistence;
using EventStore.Persistence.SqlPersistence.SqlDialects;
using EventStore.Serialization;
using Eventstore.Utils.EventPatcher;
using Lokad.Cqrs;
using Lokad.Cqrs.AtomicStorage;

namespace Eventstore.Utils
{
    public class Prefix
    {
        public const string Django = "django";
        public const string Brewmaster = "bm";
        public const string SchedulerLegacy = "ts";
        public const string Scheduler = "sched";
    }
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
        private bool allowEventPatcher;


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

        public EsBackEndBuilder WithEventStore(string eventStoreDbConn, bool allowEventPatching = false)
        {
            this.eventStoreDbConn = eventStoreDbConn;
            this.allowEventPatcher = allowEventPatching;
            return this;
        }

        public EsBackEndBuilder WithSenders(string storageConn, Type type, params Type[] otherTypes)
        {
            this.configureCommandSender = true;
            var types = new List<Type>(){type};
            
            this.typesToStream = new[]{type};
            this.typesToStream = this.typesToStream.Concat(otherTypes).ToArray();
            this.storageAccountConnectionString = storageConn;
            return this;
        }

        [Obsolete("Use WithSenders since it now sets up CommandSender and EventPublisher")]
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
                var passthroughConnectionFactory = new PassthroughConnectionFactory(eventStoreDbConn);
                var container = new NanoContainer();
                var wireupInstance = CustomWireup.Init(container)
                                   .LogToOutputWindow()
                                   .UsingSqlPersistence(passthroughConnectionFactory)
                                   .WithDialect(new MsSqlDialect())
                                   .EnlistInAmbientTransaction()
                                   .InitializeStorageEngine()
                                   .UsingServiceStackJsonSerialization();
                if (allowEventPatcher)
                {
                    container.Register<IPersistStreams>(
                        c =>
                        new CustomSqlPersistenceFactory(
                            passthroughConnectionFactory,
                            c.Resolve<ISerialize>(),
                            c.Resolve<ISqlDialect>(),
                            c.Resolve<TransactionScopeOption>(),
                            512).Build());
                }

                backEnd.EventStore = wireupInstance.Build();
                backEnd.Repository = new EventStoreRepository(backEnd.EventStore, new AggregateFactory(), new ConflictDetector());    
            }
            
            return backEnd;
        }




        
    }
}