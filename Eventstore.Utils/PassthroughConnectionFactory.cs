using System.Configuration;
using EventStore.Persistence.SqlPersistence;

namespace Eventstore.Utils
{
    public class PassthroughConnectionFactory: ConfigurationConnectionFactory
    {
        public PassthroughConnectionFactory(string connectionName) : base(connectionName)
        {
        }

        public PassthroughConnectionFactory(string masterConnectionName, string replicaConnectionName, int shards) : base(masterConnectionName, replicaConnectionName, shards)
        {
        }

        protected override ConnectionStringSettings GetConnectionStringSettings(string connectionName)
        {
            return new ConnectionStringSettings(connectionName, connectionName, "System.Data.SqlClient");
        }
    }
}