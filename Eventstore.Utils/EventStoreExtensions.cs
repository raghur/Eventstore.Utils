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
    }
}