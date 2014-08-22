using System;
using System.Collections.Generic;
using System.Linq;
using EventStore;

namespace Eventstore.Utils
{
    public static class EventBodyExtensions
    {
        public static IEnumerable<T> Events<T>(this IStoreEvents es, Func<T, bool> predicate = null,  DateTime? @from = null)
        {
            predicate = predicate ?? (t => true);
            return es.EventMessages<T>(@from)
                     .Select(e => (T) e.Body)
                     .Where(predicate);
        }

        public static IEnumerable<object> Events(this IStoreEvents es, DateTime? @from = null, params Type[] types)
        {
            return es.EventMessages(@from, types)
                     .Select(em => em.Body);
        }

        public static IEnumerable<object> Events(this IStoreEvents es, Guid aggregateId, DateTime? @from = null, params Type[] types)
        {
            return es.EventMessages(aggregateId, @from, types)
                     .Select(em => em.Body);
        }
    }
}