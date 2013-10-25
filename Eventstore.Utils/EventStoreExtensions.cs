using System;
using System.Collections.Generic;
using System.Linq;
using EventStore;

namespace Eventstore.Utils
{
    public static class EventStoreExtensions
    {
        public static IEnumerable<T> Events<T>(this IStoreEvents es, Func<T, bool> predicate = null,  DateTime? @from = null)
        {
            predicate = predicate ?? (t => true);
            return es.EventMessages<T>(from)
                     .Select(e => (T) e.Body)
                     .Where(predicate);
        }

        public static IEnumerable<EventMessage> EventMessages<T>(this IStoreEvents es, DateTime? @from = null)
        {
            return es.Events(c => c.Events.Any(e => e.Body.GetType() == typeof(T)), e => e.Body.GetType() == typeof(T), @from);

        }

        public static IEnumerable<Commit> Commits<T>(this IStoreEvents es, DateTime? @from = null)
        {
            return es.Commits(c => c.Events.Any(e => e.Body.GetType() == typeof(T)), @from);
        }

        public static IEnumerable<Commit> Commits(this IStoreEvents es, params Type[] eventTypes)
        {
            return es.Commits(c => c.Events.Any(e => eventTypes.Contains(e.Body.GetType())));
        }


        public static IEnumerable<EventMessage> Events(this IStoreEvents es,
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
                     .SelectMany(c => c.Events.Where(eventFilter));
        }

        public static IEnumerable<Commit> Commits(this IStoreEvents es,
                                                  Func<Commit, bool> commitFilter = null,
                                                  DateTime? @from = null)
        {
            commitFilter = commitFilter ?? (c => true);
            return es.Advanced
                     .GetFrom(@from ?? DateTime.MinValue)
                     .Where(commitFilter)
                     .OrderBy(c => c.CommitSequence);
        }


    }
}