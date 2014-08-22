using System;
using System.Collections.Generic;
using System.Linq;
using EventStore;
using Lokad.Cqrs;


namespace Eventstore.Utils
{
    public static class EventStoreExtensions
    {
        public static void RewriteCommits<T>(this IStoreEvents eventStore, Func<Commit, T, bool> updateFn) where T:class
        {
            var commitList = eventStore.Commits<T>();
            commitList.ToList().ForEach(c =>
                {
                    var wasModified = false;
                    var modifiedEvents = c.Events.Select(em =>
                        {
                            if (em.Body is T && updateFn(c, em.Body as T))
                            {
                                wasModified = true;
                            }
                            return em;
                        }).ToList();
                    if (!wasModified) return;
                    c.Events.Clear();
                    c.Events.AddRange(modifiedEvents);
                    eventStore.Advanced.Commit(c);
                });
        }

        public static void ReplayEvents(this IEnumerable<object> handlers, IEnumerable<EventMessage> events)
        {
            var eventHandlerChain = new RedirectToDynamicEvent();
            handlers.ToList().ForEach(eventHandlerChain.WireToWhen);
            events.ToList().ForEach(ev =>
                {
                    if (ev != null)
                    {
                        eventHandlerChain.InvokeEvent(ev.Body);
                    }
                });
        }

        public static void ReplayEvents(this IEnumerable<object> handlers, IEnumerable<object> events)
        {
            var eventHandlerChain = new RedirectToDynamicEvent();
            handlers.ToList().ForEach(eventHandlerChain.WireToWhen);
            events.ToList().ForEach(ev =>
            {
                if (ev != null)
                {
                    eventHandlerChain.InvokeEvent(ev);
                }
            });
        }

        public static IEnumerable<T> Events<T>(this IStoreEvents es, Func<T, bool> predicate = null,  DateTime? @from = null)
        {
            predicate = predicate ?? (t => true);
            return es.EventMessages<T>(from)
                     .Select(e => (T) e.Body)
                     .Where(predicate);
        }

        public static IEnumerable<object> Events(this IStoreEvents es, DateTime? @from = null, params Type[] types)
        {
            return es.EventMessages(from, types)
                     .Select(em => em.Body);
        }

        public static IEnumerable<object> Events(this IStoreEvents es, Guid aggregateId, DateTime? @from = null, params Type[] types)
        {
            return es.EventMessages(aggregateId, @from, types)
                     .Select(em => em.Body);
        }

        public static IEnumerable<EventMessage> EventMessages<T>(this IStoreEvents es, DateTime? @from = null)
        {
            return es.EventMessages(c => c.Events.Any(e => e.Body.GetType() == typeof(T)), e => e.Body.GetType() == typeof(T), @from);

        }

        public static IEnumerable<EventMessage> EventMessages(this IStoreEvents es, Guid aggregateId, DateTime? @from = null, params Type[] types)
        {
            return es.EventMessages(c => c.StreamId == aggregateId,
                                                e => types.Any(t => t == e.Body.GetType()), @from);

        }

        public static IEnumerable<EventMessage> EventMessages(this IStoreEvents es, DateTime? @from = null, params Type[] types)
        {
            return es.EventMessages(c => c.Events.Select(e => e.Body.GetType()).Intersect(types).Any() ,
                                                e => types.Any(t => t == e.Body.GetType()), @from);

        }

        public static IEnumerable<EventMessage> EventMessages(this IStoreEvents es, Guid aggregateId, DateTime? @from = null)
        {
            return es.EventMessages(c => c.StreamId == aggregateId, null, @from);
        }

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

        
        public static IEnumerable<Commit> Commits<T>(this IStoreEvents es, Func<T, bool> cond  = null, DateTime? @from = null)
        {
            cond = cond ?? (t => true);
            return es.Commits(c => c.Events.Any(e => e.Body.GetType() == typeof (T) && cond((T) e.Body)), @from);

        }

        public static IEnumerable<Commit> Commits(this IStoreEvents es, params Type[] eventTypes)
        {
            return es.Commits(c => c.Events.Any(e => eventTypes.Contains(e.Body.GetType())));
        }


        
        public static IEnumerable<Commit> Commits(this IStoreEvents es,
                                                  Func<Commit, bool> commitFilter = null,
                                                  DateTime? @from = null)
        {
            commitFilter = commitFilter ?? (c => true);
            return es.Advanced
                     .GetFrom(@from ?? DateTime.MinValue)
                     .Where(commitFilter)
                     .OrderBy(c => c.CommitSequence)
                     .ToList();
        }


    }
}