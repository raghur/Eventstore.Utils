﻿using System;
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
        public static IEnumerable<Commit> RewriteCommits<T>(this IStoreEvents eventStore, Func<Commit, T, bool> updateFn, Action<EventMessage> dumper = null) where T : class
        {
            var dryRun = dumper != null;
            var modifiedCommits = new List<Commit>();
            dumper = dumper ?? (e => { });
            var commitList = eventStore.Commits<T>();
            commitList.ToList().ForEach(c =>
                {
                    var wasModified = false;
                    var modifiedEvents = c.Events.Select(em =>
                        {
                            if (em.Body is T && updateFn(c, em.Body as T))
                            {
                                dumper(em);
                                modifiedCommits.Add(c);
                                wasModified = true;
                            }
                            return em;
                        }).ToList();
                    if (!wasModified) return;
                    if (dryRun) return;
                    c.Events.Clear();
                    c.Events.AddRange(modifiedEvents);
                    eventStore.Advanced.Commit(c);
                });
            return modifiedCommits.OrderBy(c => c.CommitStamp);
        }

        public static IEnumerable<Commit> RewriteCommits(this IStoreEvents eventStore, Func<Commit, object, bool> updateFn, Action<EventMessage> dumper = null) 
        {
            var commitList = eventStore.Commits();
            return eventStore.RewriteCommits(commitList, updateFn,dumper);
        }

        public static IEnumerable<Commit> RewriteCommits(this IStoreEvents eventStore, IEnumerable<Commit> commitList, Func<Commit, object, bool> updateFn, Action<EventMessage> dumper = null)
        {
            var dryRun = dumper != null;
            dumper = dumper ?? (e => { });
            var modifiedCommits = new List<Commit>();
            commitList.ToList().ForEach(c =>
            {
                var wasModified = false;
                var modifiedEvents = c.Events.Select(em =>
                {
                    if (updateFn(c, em.Body))
                    {
                        dumper(em);
                        modifiedCommits.Add(c);
                        wasModified = true;
                    }
                    return em;
                }).ToList();
                if (!wasModified) return;
                if (dryRun) return;
                c.Events.Clear();
                c.Events.AddRange(modifiedEvents);
                eventStore.Advanced.Commit(c);
            });
            return modifiedCommits.OrderBy(c => c.CommitStamp);
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