using System;
using System.Collections.Generic;
using System.Linq;
using EventStore;
using Lokad.Cqrs;


namespace Eventstore.Utils
{
   public static class EventStoreExtensions
    {
        public static IEnumerable<Commit> RewriteCommits(this IStoreEvents eventStore, IEnumerable<Commit> commits, bool dryRun = true)
        {
            if (dryRun) return commits;
            var rewriteCommits = commits as IList<Commit> ?? commits.ToList();
            rewriteCommits.ToList().ForEach(c => eventStore.Advanced.Commit(c));
            return rewriteCommits.OrderBy(c => c.CommitStamp);
        }
        public static IEnumerable<Commit> RewriteCommits<T>(this IStoreEvents eventStore, Func<Commit, T, bool> updateFn, bool dryRun = true) where T : class
        {
            var modifiedCommits = new List<Commit>();
            var commitList = eventStore.Commits<T>();
            commitList.ToList().ForEach(c =>
                {
                    var wasModified = false;
                    var modifiedEvents = c.Events.Select(em =>
                        {
                            if (em.Body is T && updateFn(c, em.Body as T))
                            {
                                modifiedCommits.Add(c);
                                wasModified = true;
                            }
                            return em;
                        }).ToList();
                    if (!wasModified) return;
                    c.Events.Clear();
                    c.Events.AddRange(modifiedEvents);
                });
            return  eventStore.RewriteCommits(modifiedCommits, dryRun);
        }

        public static IEnumerable<Commit> RewriteCommits(this IStoreEvents eventStore, Func<Commit, object, bool> updateFn, bool dryRun = true) 
        {
            var commitList = eventStore.Commits();
            return eventStore.RewriteCommits(commitList, updateFn,dryRun);
        }

        public static IEnumerable<Commit> RewriteCommits(this IStoreEvents eventStore, IEnumerable<Commit> commitList, Func<Commit, object, bool> updateFn, bool dryRun = true)
        {
            var modifiedCommits = new List<Commit>();
            commitList.ToList().ForEach(c =>
            {
                var wasModified = false;
                var modifiedEvents = c.Events.Select(em =>
                {
                    if (updateFn(c, em.Body))
                    {
                        modifiedCommits.Add(c);
                        wasModified = true;
                    }
                    return em;
                }).ToList();
                if (!wasModified) return;
                c.Events.Clear();
                c.Events.AddRange(modifiedEvents);
            });
            return eventStore.RewriteCommits(modifiedCommits, dryRun);
        }

       
        public static IEnumerable<KeyValuePair<EventMessage, Exception>> ReplayEvents(this IEnumerable<object> handlers, IEnumerable<EventMessage> events)
        {
            var eventHandlerChain = new RedirectToDynamicEvent();
            var exceptions = new List<KeyValuePair<EventMessage, Exception>>();
            handlers.ToList().ForEach(eventHandlerChain.WireToWhen);
            events.ToList().ForEach(ev =>
                {
                    if (ev != null)
                    {
                        try
                        {
                            eventHandlerChain.InvokeEvent(ev.Body);
                        }
                        catch (Exception ex)
                        {
                            exceptions.Add(new KeyValuePair<EventMessage, Exception>(ev, ex));
                        }
                    }
                });
            return exceptions;
        }

        public static IEnumerable<KeyValuePair<object, Exception>> ReplayEvents(this IEnumerable<object> handlers, IEnumerable<object> events)
        {
            var exceptions = new List<KeyValuePair<object, Exception>>();
            var eventHandlerChain = new RedirectToDynamicEvent();
            handlers.ToList().ForEach(eventHandlerChain.WireToWhen);
            events.ToList().ForEach(ev =>
            {
                if (ev != null)
                {
                    try
                    {
                        eventHandlerChain.InvokeEvent(ev);
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(new KeyValuePair<object, Exception>(ev, ex));
                    }
                }
            });
            return exceptions;
        }
    }
}