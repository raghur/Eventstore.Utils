using System;
using System.Configuration;
using EventStore.Persistence.SqlPersistence;

namespace Eventstore.Utils
{
    public class AzureConfigurationConnectionFactory : ConfigurationConnectionFactory
    {
        string connectionString;
        public AzureConfigurationConnectionFactory(string connectionName)
            : base(connectionName)
        {
        }

        public AzureConfigurationConnectionFactory(string connectionName, string connectionString)
            : base(connectionName)
        {
            this.connectionString = connectionString;
        }

        public AzureConfigurationConnectionFactory(string masterConnectionName, string replicaConnectionName, int shards)
            : base(masterConnectionName, replicaConnectionName, shards)
        {
        }

        protected override ConnectionStringSettings GetConnectionStringSettings(string connectionName)
        {
            Console.Write(connectionName);
            Console.Write(connectionString);
            var settings = new ConnectionStringSettings(connectionName, connectionString, "System.Data.SqlClient");

            return settings;
        }

    }
}