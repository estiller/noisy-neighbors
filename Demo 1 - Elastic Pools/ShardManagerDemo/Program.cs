using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Microsoft.Azure.SqlDatabase.ElasticScale.ShardManagement;
using Microsoft.Azure.SqlDatabase.ElasticScale.ShardManagement.Schema;
using ShardManagerDemo.Utils;

namespace ShardManagerDemo
{
    class Program
    {
        private const string ShardNameFormat = "Tenant_{0}";
        private const string InitializeShardScriptFile = "InitializeShard.sql";
        private const int TenantCount = 4;

        static void Main()
        {
            ShardMapManager shardMapManager = CreateShardMapManager();
            ListShardMap<int> shardMap = CreateShardMapAndInitializeShards(shardMapManager);
            ExecuteDataDependentRoutingQuery(shardMap, Configuration.GetCredentialsConnectionString());
        }

        #region Create Shard Map Manager

        private static ShardMapManager CreateShardMapManager()
        {
            // Create shard map manager database
            if (!SqlDatabaseUtils.DatabaseExists(Configuration.ShardMapManagerServerName, Configuration.ShardMapManagerDatabaseName))
            {
                SqlDatabaseUtils.CreateDatabase(Configuration.ShardMapManagerServerName, Configuration.ShardMapManagerDatabaseName, null);
            }

            // Create shard map manager
            string shardMapManagerConnectionString =
                Configuration.GetConnectionString(
                    Configuration.ShardMapManagerServerName,
                    Configuration.ShardMapManagerDatabaseName);

            return ShardManagementUtils.CreateOrGetShardMapManager(shardMapManagerConnectionString);
        }

        #endregion

        #region Create Shard Map and Shards

        private static ListShardMap<int> CreateShardMapAndInitializeShards(ShardMapManager shardMapManager)
        {
            // Create shard map
            ListShardMap<int> shardMap = ShardManagementUtils.CreateOrGetListShardMap<int>(shardMapManager, Configuration.ShardMapName);

            // Create schema info so that the split-merge service can be used to move data in sharded tables
            // and reference tables.
            CreateSchemaInfo(shardMapManager, shardMap.Name);


            // If there are no shards, add shards
            if (!shardMap.GetShards().Any())
            {
                for (int i = 0; i < TenantCount; i++)
                {
                    CreateShard(shardMap, i);
                }
            }
            return shardMap;
        }

        /// <summary>
        /// Creates schema info for the schema defined in InitializeShard.sql.
        /// </summary>
        private static void CreateSchemaInfo(ShardMapManager shardMapManager, string shardMapName)
        {
            // Create schema info
            SchemaInfo schemaInfo = new SchemaInfo();
            schemaInfo.Add(new ReferenceTableInfo("Regions"));
            schemaInfo.Add(new ReferenceTableInfo("Products"));
            schemaInfo.Add(new ShardedTableInfo("Customers", "CustomerId"));
            schemaInfo.Add(new ShardedTableInfo("Orders", "CustomerId"));

            // Register it with the shard map manager for the given shard map name
            shardMapManager.GetSchemaInfoCollection().Add(shardMapName, schemaInfo);
        }

        private static void CreateShard(ListShardMap<int> shardMap, int idForNewShard)
        {
            // Create a new shard, or get an existing empty shard (if a previous create partially succeeded).
            Shard shard = CreateOrGetEmptyShard(shardMap);

            // Create a mapping to that shard.
            PointMapping<int> mappingForNewShard = shardMap.CreatePointMapping(new PointMappingCreationInfo<int>(idForNewShard, shard, MappingStatus.Online));
            Console.WriteLine("Mapped range {0} to shard {1}", mappingForNewShard.Value, shard.Location.Database);
        }

        private static Shard CreateOrGetEmptyShard(ListShardMap<int> shardMap)
        {
            // Get an empty shard if one already exists, otherwise create a new one
            Shard shard = FindEmptyShard(shardMap);
            if (shard == null)
            {
                // No empty shard exists, so create one

                // Choose the shard name
                string databaseName = string.Format(ShardNameFormat, shardMap.GetShards().Count());

                // Only create the database if it doesn't already exist. It might already exist if
                // we tried to create it previously but hit a transient fault.
                if (!SqlDatabaseUtils.DatabaseExists(Configuration.ShardMapManagerServerName, databaseName))
                {
                    SqlDatabaseUtils.CreateDatabase(Configuration.ShardMapManagerServerName, databaseName, Configuration.ElasticPoolName);
                }

                // Create schema and populate reference data on that database
                // The initialize script must be idempotent, in case it was already run on this database
                // and we failed to add it to the shard map previously
                SqlDatabaseUtils.ExecuteSqlScript(
                    Configuration.ShardMapManagerServerName, databaseName, InitializeShardScriptFile);

                // Add it to the shard map
                ShardLocation shardLocation = new ShardLocation(Configuration.ShardMapManagerServerName, databaseName);
                shard = ShardManagementUtils.CreateOrGetShard(shardMap, shardLocation);
            }

            return shard;
        }

        private static Shard FindEmptyShard(ListShardMap<int> shardMap)
        {
            // Get all shards in the shard map
            IEnumerable<Shard> allShards = shardMap.GetShards();

            // Get all mappings in the shard map
            IEnumerable<PointMapping<int>> allMappings = shardMap.GetMappings();

            // Determine which shards have mappings
            HashSet<Shard> shardsWithMappings = new HashSet<Shard>(allMappings.Select(m => m.Shard));

            // Get the first shard (ordered by name) that has no mappings, if it exists
            return allShards.OrderBy(s => s.Location.Database).FirstOrDefault(s => !shardsWithMappings.Contains(s));
        }

        #endregion

        #region Data Dependent Routing Query

        private static readonly Random Random = new Random();

        public static void ExecuteDataDependentRoutingQuery(ListShardMap<int> shardMap, string credentialsConnectionString)
        {
            // A real application handling a request would need to determine the request's customer ID before connecting to the database.
            // Since this is a demo app, we just choose a random key out of the range that is mapped. Here we assume that the ranges
            // start at 0, are contiguous, and are bounded (i.e. there is no range where HighIsMax == true)
            int customerId = Random.Next(TenantCount);
            string customerName = customerId.ToString();
            int regionId = 0;
            int productId = 0;

            AddCustomer(
                shardMap,
                credentialsConnectionString,
                customerId,
                customerName,
                regionId);

            AddOrder(
                shardMap,
                credentialsConnectionString,
                customerId,
                productId);
        }

        /// <summary>
        /// Adds a customer to the customers table (or updates the customer if that id already exists).
        /// </summary>
        private static void AddCustomer(
            ShardMap shardMap,
            string credentialsConnectionString,
            int customerId,
            string name,
            int regionId)
        {
            // Open and execute the command with retry for transient faults. Note that if the command fails, the connection is closed, so
            // the entire block is wrapped in a retry. This means that only one command should be executed per block, since if we had multiple
            // commands then the first command may be executed multiple times if later commands fail.
            SqlDatabaseUtils.SqlRetryPolicy.ExecuteAction(() =>
            {
                // Looks up the key in the shard map and opens a connection to the shard
                using (SqlConnection conn = shardMap.OpenConnectionForKey(customerId, credentialsConnectionString))
                {
                    // Create a simple command that will insert or update the customer information
                    SqlCommand cmd = conn.CreateCommand();
                    cmd.CommandText = @"
                    IF EXISTS (SELECT 1 FROM Customers WHERE CustomerId = @customerId)
                        UPDATE Customers
                            SET Name = @name, RegionId = @regionId
                            WHERE CustomerId = @customerId
                    ELSE
                        INSERT INTO Customers (CustomerId, Name, RegionId)
                        VALUES (@customerId, @name, @regionId)";
                    cmd.Parameters.AddWithValue("@customerId", customerId);
                    cmd.Parameters.AddWithValue("@name", name);
                    cmd.Parameters.AddWithValue("@regionId", regionId);
                    cmd.CommandTimeout = 60;

                    // Execute the command
                    cmd.ExecuteNonQuery();
                }
            });
        }

        /// <summary>
        /// Adds an order to the orders table for the customer.
        /// </summary>
        private static void AddOrder(
            ShardMap shardMap,
            string credentialsConnectionString,
            int customerId,
            int productId)
        {
            SqlDatabaseUtils.SqlRetryPolicy.ExecuteAction(() =>
            {
                // Looks up the key in the shard map and opens a connection to the shard
                using (SqlConnection conn = shardMap.OpenConnectionForKey(customerId, credentialsConnectionString))
                {
                    // Create a simple command that will insert a new order
                    SqlCommand cmd = conn.CreateCommand();

                    // Create a simple command
                    cmd.CommandText = @"INSERT INTO dbo.Orders (CustomerId, OrderDate, ProductId)
                                        VALUES (@customerId, @orderDate, @productId)";
                    cmd.Parameters.AddWithValue("@customerId", customerId);
                    cmd.Parameters.AddWithValue("@orderDate", DateTime.Now.Date);
                    cmd.Parameters.AddWithValue("@productId", productId);
                    cmd.CommandTimeout = 60;

                    // Execute the command
                    cmd.ExecuteNonQuery();
                }
            });
        }

        #endregion
    }
}
