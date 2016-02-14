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
            if (types.Length > 0)
            {
                return es.EventMessages(@from, types)
                         .Select(em => em.Body);
            }
            return es.EventMessages(@from).Select(em => em.Body);
        }

        public static IEnumerable<object> Events(this IStoreEvents es, Guid aggregateId, DateTime? @from = null, params Type[] types)
        {
            if (types.Length > 0)
            {
                return es.EventMessages(aggregateId, @from, types)
                         .Select(em => em.Body);
            }
            return es.EventMessages(aggregateId, @from)
                      .Select(em => em.Body);
        }

        // Delete Events 
        public static IEnumerable<Commit> DeleteEvents(this IStoreEvents eventStore, IEnumerable<Commit> commitList, Func<Commit, object, bool> deleteCond, bool dryRun = true)
        {
            var deletedCommits = new List<Commit>();
            commitList.ToList().ForEach(c =>
            {
                var wasRemoved = false;
                var modifiedEvents = c.Events.Select(em =>
                {
                    if (deleteCond(c, em.Body))
                    {
                        deletedCommits.Add(c);
                        wasRemoved = true;
                        return null;
                    }
                    return em;
                }).Where(em => em != null).ToList();
                if (!wasRemoved) return;
                if (dryRun) return;
                c.Events.Clear();
                c.Events.AddRange(modifiedEvents);
                eventStore.Advanced.Commit(c);
            });
            return deletedCommits.OrderBy(c => c.CommitStamp);
        }
    }
}