using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using EventStore;
using EventStore.Conversion;
using EventStore.Dispatcher;
using EventStore.Persistence;
using EventStore.Persistence.InMemoryPersistence;

namespace Eventstore.Utils.EventPatcher
{
    public class CustomWireup : Wireup
    {
        protected CustomWireup(NanoContainer container) : base(container)
        {
        }

        protected CustomWireup(Wireup inner) : base(inner)
        {
        }

        public static CustomWireup Init(NanoContainer container)
        {
            container.Register<TransactionScopeOption>(TransactionScopeOption.Suppress);
            container.Register<IPersistStreams>((IPersistStreams) new InMemoryPersistenceEngine());
            container.Register<IStoreEvents>(new Func<NanoContainer, IStoreEvents>(CustomWireup.BuildEventStore));
            return new CustomWireup(container);
        }

        private static IStoreEvents BuildEventStore(NanoContainer context)
        {
            ICollection<IPipelineHook> collection =
                (ICollection<IPipelineHook>)
                Enumerable.ToArray<IPipelineHook>(
                    Enumerable.Where<IPipelineHook>(
                        Enumerable.Concat<IPipelineHook>((IEnumerable<IPipelineHook>) new IPipelineHook[3]
                            {
                                (IPipelineHook)
                                (context.Resolve<TransactionScopeOption>() == TransactionScopeOption.Suppress
                                     ? new OptimisticPipelineHook()
                                     : (OptimisticPipelineHook) null),
                                (IPipelineHook)
                                new DispatchSchedulerPipelineHook(context.Resolve<IScheduleDispatches>()),
                                (IPipelineHook) context.Resolve<EventUpconverterPipelineHook>()
                            },
                                                         (IEnumerable<IPipelineHook>)
                                                         (context.Resolve<ICollection<IPipelineHook>>() ??
                                                          (ICollection<IPipelineHook>) new IPipelineHook[0])),
                        (Func<IPipelineHook, bool>) (x => x != null)));
            return
                (IStoreEvents)
                new OptimisticEventStore(context.Resolve<IPersistStreams>(), (IEnumerable<IPipelineHook>) collection);
        }
    }
}