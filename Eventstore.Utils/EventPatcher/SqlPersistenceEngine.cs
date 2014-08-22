using System;
using System.Collections.Generic;
using System.Transactions;
using EventStore;
using EventStore.Persistence.SqlPersistence;
using EventStore.Serialization;

namespace Eventstore.Utils.EventPatcher
{
    public class SqlPersistenceEngine: EventStore.Persistence.SqlPersistence.SqlPersistenceEngine
    {
        private ISqlDialect dialect;
        private ISerialize serializer;
        private const string UpdateStmt = 
            @"Update Commits
                                            Set
                                                Items = @Items,
                                                Headers = @Headers,
                                                Payload = @Payload
                                            Where
                                                StreamId = @StreamId and
                                                CommitSequence = @CommitSequence";
        public SqlPersistenceEngine(IConnectionFactory connectionFactory, ISqlDialect dialect, ISerialize serializer, TransactionScopeOption scopeOption, int pageSize) : base(connectionFactory, dialect, serializer, scopeOption, pageSize)
        {
            this.dialect = dialect;
            this.serializer = serializer;
        }

        public override void Commit(Commit attempt)
        {
          PersistExistingCommit(attempt);
            
            
        }

        private void PersistExistingCommit(Commit attempt)
        {
            this.ExecuteCommand<int>(attempt.StreamId, (Func<IDbStatement, int>)(cmd =>
                {
                    cmd.AddParameter(this.dialect.StreamId, (object)attempt.StreamId);
                    cmd.AddParameter(this.dialect.Items, (object)attempt.Events.Count);
                    cmd.AddParameter(this.dialect.CommitSequence, (object)attempt.CommitSequence);
                    cmd.AddParameter(this.dialect.Headers, (object)SerializationExtensions.Serialize<Dictionary<string, object>>(this.serializer, attempt.Headers));
                    cmd.AddParameter(this.dialect.Payload, (object)SerializationExtensions.Serialize<List<EventMessage>>(this.serializer, attempt.Events));
                    return cmd.ExecuteNonQuery(UpdateStmt);
                }));
        }
    }
}