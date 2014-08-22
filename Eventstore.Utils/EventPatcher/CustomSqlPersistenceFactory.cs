using System.Transactions;
using EventStore.Persistence.SqlPersistence;
using EventStore.Serialization;

namespace Eventstore.Utils.EventPatcher
{
    public class CustomSqlPersistenceFactory: EventStore.Persistence.SqlPersistence.SqlPersistenceFactory
    {
        private readonly IConnectionFactory factory;
        private readonly ISerialize serializer;
        private readonly ISqlDialect dialect;
        private readonly TransactionScopeOption scopeOption;
        private readonly int pageSize;

        public CustomSqlPersistenceFactory(IConnectionFactory factory, ISerialize serializer, ISqlDialect dialect, TransactionScopeOption scopeOption, int pageSize) : base(factory, serializer, dialect, scopeOption, pageSize)
        {
            this.factory = factory;
            this.serializer = serializer;
            this.dialect = dialect;
            this.scopeOption = scopeOption;
            this.pageSize = pageSize;
        }

        public override EventStore.Persistence.IPersistStreams Build()
        {
            return new SqlPersistenceEngine(factory, dialect, serializer,scopeOption, pageSize);
        }
    }
}