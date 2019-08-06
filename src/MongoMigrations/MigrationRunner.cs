namespace MongoMigrations
{
    using Microsoft.Extensions.Logging;
    using MongoDB.Bson;
    using MongoDB.Bson.Serialization;
    using MongoDB.Driver;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class MigrationRunner
    {
        private readonly ILogger _logger;

        static MigrationRunner()
        {
            Init();
        }

        public static void Init()
        {
            BsonSerializer.RegisterSerializer(typeof(MigrationVersion), new MigrationVersionSerializer());
        }

        public MigrationRunner(
            string mongoServerLocation,
            string databaseName, 
            ILogger<MigrationRunner> logger = null
        )
            : this(new MongoClient(mongoServerLocation), databaseName, logger)
        {
        }

        public MigrationRunner(
            IMongoClient client,
            string databaseName,
            ILogger<MigrationRunner> logger = null)
        {
            _logger = logger ?? NullLogger<MigrationRunner>.Instance;
            Client = client;
            DatabaseName = databaseName;
            DatabaseStatus = new DatabaseMigrationStatus(this);
            MigrationLocator = new MigrationLocator();
        }

        private async Task CloneCollections(
            IMongoDatabase sourceDatabase, 
            IMongoDatabase destDatabase,
            ILogger<MigrationRunner> logger = null
        )
        {
            var collectionNames = await sourceDatabase.ListCollectionNamesAsync();
            var copyBlock = new List<BsonDocument>(1000); // arbitrary
            await collectionNames.ForEachAsync(async (collectionName) =>
            {
                if (_logger != null)
                {
                    _logger.LogInformation($"Copying collection {collectionName}");
                }

                var sourceCollection = sourceDatabase.GetCollection<BsonDocument>(collectionName);
                var destCollection = destDatabase.GetCollection<BsonDocument>(collectionName);
                await destCollection.DeleteManyAsync(FilterDefinition<BsonDocument>.Empty);

                var sourceCursor = await sourceCollection.Find(FilterDefinition<BsonDocument>.Empty).ToCursorAsync();
                await sourceCursor.ForEachAsync(async (doc) =>
                {
                    copyBlock.Add(doc);
                    if (copyBlock.Count >= 1000)
                    {
                        await destCollection.InsertManyAsync(copyBlock);
                        copyBlock.Clear();
                    }
                });
                if (copyBlock.Count > 0)
                {
                    await destCollection.InsertManyAsync(copyBlock);
                    copyBlock.Clear();
                }
            });
        }

        public async Task BackupAndRestore()
        {
            var database = Client.GetDatabase(DatabaseName);

            // Check if a backup database exists
            string backupDatabaseName = DatabaseName + "_MigrationBackup";
            bool backupExists = (await (await Client.ListDatabaseNamesAsync()).ToListAsync()).Any(
                x => String.Equals(x, DatabaseName, StringComparison.InvariantCultureIgnoreCase));

            // Get a reference to a backup database (creating if it doesn't exist)
            var backupDatabase = Client.GetDatabase(backupDatabaseName);
            if (backupExists)
            {
                // Restore collections from an existing database
                _logger.LogWarning($"Restoring database from backup {backupDatabaseName}");
                await CloneCollections(backupDatabase, database);

                // Clean the entire database up
                await Client.DropDatabaseAsync(backupDatabaseName);
            }

            // Make a fresh backup
            await CloneCollections(database, backupDatabase);
        }

        public IMongoClient Client { get; set; }
        public string DatabaseName { get; set; }
        public MigrationLocator MigrationLocator { get; set; }
        public DatabaseMigrationStatus DatabaseStatus { get; set; }

        public virtual Task UpdateToLatestAsync()
        {
            _logger.LogInformation(WhatWeAreUpdating() + " to latest...");
            return UpdateToAsync(MigrationLocator.LatestVersion());
        }

        private string WhatWeAreUpdating()
        {
            return string.Format("Updating server(s) \"{0}\" for database \"{1}\"",
                ServerAddresses(), DatabaseName);
        }

        private string ServerAddresses()
        {
            return String.Join(",", Client.Settings.Servers.Select(s => s.Host));
        }

        protected virtual async Task ApplyMigrationsAsync(IEnumerable<Migration> migrations)
        {
            // If there are any experimental migrations, then assume we are in development mode
            // and back the database up
            bool experimentalMigrations = migrations.Any(
                x => ExcludeExperimentalMigrations.HasExperimentalAttribute(x));
            if (experimentalMigrations)
            {
                await BackupAndRestore();
            }

            var database = Client.GetDatabase(DatabaseName);
            foreach (var migration in migrations)
            {
                await ApplyMigrationAsync(database, migration);
            }
        }

        protected virtual async Task ApplyMigrationAsync(IMongoDatabase database, Migration migration)
        {
            _logger.LogInformation(new
            {
                Message = "Applying migration",
                migration.Version,
                migration.Description,
                DatabaseName
            }.ToString());

            var appliedMigration = await DatabaseStatus.StartMigrationAsync(migration);
            migration.Database = database;
            try
            {
                await migration.UpdateAsync();
            }
            catch (Exception exception)
            {
                OnMigrationException(migration, exception);
            }
            await DatabaseStatus.CompleteMigrationAsync(appliedMigration);
        }

        protected virtual void OnMigrationException(Migration migration, Exception exception)
        {
            var message = new
            {
                Message = "Migration failed to be applied: " + exception.Message,
                migration.Version,
                Name = migration.GetType(),
                migration.Description,
                DatabaseName
            };
            _logger.LogError(exception, message.ToString());
            throw new MigrationException(message.ToString(), exception);
        }

        public virtual async Task UpdateToAsync(MigrationVersion updateToVersion)
        {
            var unapplied = await DatabaseStatus.GetUnappliedMigrationsAsync();

            var first = unapplied.FirstOrDefault();
            if (first == null)
            {
                return;
            }

            _logger.LogInformation(new
            {
                Message = WhatWeAreUpdating(),
                firstUnapplied = first,
                updateToVersion,
                DatabaseName
            }.ToString()); ;

            await ApplyMigrationsAsync(unapplied);
        }
    }
}