namespace MongoMigrations
{
	using System;
	using System.Linq;
    using System.Threading.Tasks;
    using MongoDB.Driver;

	public class DatabaseMigrationStatus
	{
		private readonly MigrationRunner _Runner;

		public string VersionCollectionName = "DatabaseVersion";

		public DatabaseMigrationStatus(MigrationRunner runner)
		{
			_Runner = runner;
		}

		public virtual IMongoCollection<AppliedMigration> GetMigrationsApplied()
		{
			return _Runner.Client.GetDatabase(_Runner.DatabaseName)
                .GetCollection<AppliedMigration>(VersionCollectionName);
		}

		public virtual async Task<bool> IsNotLatestVersionAsync()
		{
			return _Runner.MigrationLocator.LatestVersion()
			       != await GetVersionAsync();
		}

		public virtual async Task ThrowIfNotLatestVersionAsync()
		{
			if (!await IsNotLatestVersionAsync())
			{
				return;
			}
			var databaseVersion = await GetVersionAsync();
			var migrationVersion = _Runner.MigrationLocator.LatestVersion();
			throw new ApplicationException("Database is not the expected version, database is at version: " + 
                databaseVersion + ", migrations are at version: " + migrationVersion);
		}

		public virtual async Task<MigrationVersion> GetVersionAsync()
		{
			var lastAppliedMigration = await GetLastAppliedMigrationAsync();
			return lastAppliedMigration == null
			       	? MigrationVersion.Default()
			       	: lastAppliedMigration.Version;
		}

		public virtual async Task<AppliedMigration> GetLastAppliedMigrationAsync()
		{
            // Fetch the lot since we need to sort by the parsed version number
            // (serialize it as 3 ints??)
            var migrations = await GetMigrationsApplied()
                .Find(FilterDefinition<AppliedMigration>.Empty)
                .ToListAsync();
            return migrations
                .OrderByDescending(v => v.Version)
                .FirstOrDefault();
		}

		public virtual async Task<AppliedMigration> StartMigrationAsync(Migration migration)
		{
			var appliedMigration = new AppliedMigration(migration);
			await GetMigrationsApplied().InsertOneAsync(appliedMigration);
			return appliedMigration;
		}

		public virtual Task CompleteMigrationAsync(AppliedMigration appliedMigration)
		{
            return GetMigrationsApplied().UpdateOneAsync(
                x => x.Version == appliedMigration.Version,
                Builders<AppliedMigration>.Update.Set(x => x.CompletedOn, DateTime.UtcNow));
		}

		public virtual async Task MarkUpToVersionAsync(MigrationVersion version)
		{
            var migrations = _Runner.MigrationLocator
                .GetAllMigrations()
                .Where(m => m.Version <= version);
            foreach (var migration in migrations)
            {
                await MarkVersionAsync(migration.Version);
            }
		}

		public virtual Task MarkVersionAsync(MigrationVersion version)
		{
			var appliedMigration = AppliedMigration.MarkerOnly(version);
			return GetMigrationsApplied().InsertOneAsync(appliedMigration);
		}
	}
}
