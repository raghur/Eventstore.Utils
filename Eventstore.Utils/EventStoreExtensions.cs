using System;
using System.Collections.Generic;
using System.Linq;
using EventStore;
using Lokad.Cqrs;


namespace Eventstore.Utils
{
    public class Pair<TA,TB>
    {
        public TA Item1;
        public TB Item2;
    }
    public static class EventStoreExtensions
    {
        private static void NoOp<T>(T a) {}

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

       
        public static IEnumerable<Pair<EventMessage, Exception>> ReplayEvents(this IEnumerable<object> handlers, IEnumerable<EventMessage> events, Action<EventMessage, Exception> dumper = null)
        {
            dumper = dumper ?? ((o, ex) => { });
            var eventHandlerChain = new RedirectToDynamicEvent();
            var exceptions = new List<Pair<EventMessage, Exception>>();
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
                            dumper(ev, ex);
                            exceptions.Add(new Pair<EventMessage, Exception>(){Item1 = ev, Item2 = ex});
                        }
                    }
                });
            return exceptions;
        }

        public static IEnumerable<Pair<object, Exception>> ReplayEvents(this IEnumerable<object> handlers, IEnumerable<object> events, Action<object, Exception> dumper = null)
        {
            dumper = dumper ?? ((o, ex) => { });
            var exceptions = new List<Pair<object, Exception>>();
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
                        dumper(ev, ex);
                        exceptions.Add(new Pair<object, Exception>() { Item1 = ev, Item2 = ex });
                    }
                }
            });
            return exceptions;
        }
    }
}