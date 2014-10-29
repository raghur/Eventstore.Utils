using System;
using System.Collections.Generic;
using System.Linq;
using EventStore;

namespace Eventstore.Utils
{
    public static class CommitExtensions
    {
        // Get commits that have events of specific type
        public static IEnumerable<Commit> Commits<T>(this IStoreEvents es, Func<T, bool> cond  = null, DateTime? @from = null)
        {
            cond = cond ?? (t => true);
            return es.Commits(c => Enumerable.Any(c.Events, e => e.Body.GetType() == typeof (T) && cond((T) e.Body)), @from);

        }

        // Get commits that have events of specific type - aggregate filter
        public static IEnumerable<Commit> Commits(this IStoreEvents es, Guid aggId, Func<Commit, bool> cond = null, DateTime? @from = null)
        {
            cond = cond ?? (t => true);
            return es.Commits(c => c.StreamId == aggId && cond(c), @from);
        }

        // get commits having events of one or more types
        public static IEnumerable<Commit> Commits(this IStoreEvents es, params Type[] eventTypes)
        {
            return es.Commits(c => c.Events.Any(e => eventTypes.Contains(e.Body.GetType())));
        }

        // get commits for aggregate
        public static IEnumerable<Commit> Commits(this IStoreEvents es, Guid aggId)
        {
            return es.Commits(c => c.StreamId == aggId);
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

        public static void Rewrite(this IEnumerable<Commit> commits, IStoreEvents eventStore,
                                   bool dryRun = true)
        {
            eventStore.RewriteCommits(commits, (c,o) => true, dryRun);
        }
    }
}