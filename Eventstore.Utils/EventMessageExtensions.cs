using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EventStore;

namespace Eventstore.Utils
{
    public static class EventMessageExtensions
    {
        // get all events of type T
        public static IEnumerable<EventMessage> EventMessages<T>(this IStoreEvents es, DateTime? @from = null)
        {
            return es.EventMessages(c => Enumerable.Any<EventMessage>(c.Events, e => e.Body.GetType() == typeof (T)),
                                    e => e.Body.GetType() == typeof (T), @from);
        }

        // get all events of type T for aggregate
        public static IEnumerable<EventMessage> EventMessages<T>(this IStoreEvents es, Guid aggregateId,
                                                                 DateTime? @from = null)
        {
            return es.EventMessages(aggregateId, @from, typeof (T));
        }

        // get events for more than one type
        public static IEnumerable<EventMessage> EventMessages(this IStoreEvents es, DateTime? @from = null,
                                                              params Type[] types)
        {
            return es.EventMessages(c => c.Events.Select(e => e.Body.GetType()).Intersect(types).Any(),
                                    e => types.Any(t => t == e.Body.GetType()), @from);
            
        }

        // get events of more than one type for aggregate
        public static IEnumerable<EventMessage> EventMessages(this IStoreEvents es, Guid aggregateId,
                                                              DateTime? @from = null, params Type[] types)
        {
            return es.EventMessages(c => c.StreamId == aggregateId,
                                    e => types.Any(t => t == e.Body.GetType()), @from);
        }

        // get events for aggregate
        public static IEnumerable<EventMessage> EventMessages(this IStoreEvents es, Guid aggregateId,
                                                              DateTime? @from = null)
        {
            return es.EventMessages(c => c.StreamId == aggregateId, null, @from);
        }

        // get all events
        public static IEnumerable<EventMessage> EventMessages(this IStoreEvents es, DateTime? @from = null)
        {
            return es.EventMessages(null, null, @from);
        }

        public static IEnumerable<EventMessage> EventMessages(this IStoreEvents es,
                                                              Func<Commit, bool> commitFilter = null,
                                                              Func<EventMessage, bool> eventFilter = null,
                                                              DateTime? @from = null)
        {
            commitFilter = commitFilter ?? (c => true);
            eventFilter = eventFilter ?? (em => true);
            return es.Advanced
                     .GetFrom(@from ?? DateTime.MinValue)
                     .Where(commitFilter)
                     .OrderBy(c => c.CommitSequence)
                     .SelectMany(c => c.Events.Where(eventFilter))
                     .ToList();
        }
    }
}