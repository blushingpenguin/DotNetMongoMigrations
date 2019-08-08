namespace MongoMigrations
{
	using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
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

        public async Task<IEnumerable<Migration>> GetUnappliedMigrationsAsync(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var allMigrations = _Runner.MigrationLocator.GetAllMigrations(); // ordered

            var appliedMigrations = new HashSet<MigrationVersion>();
            var applied = await GetMigrationsApplied().FindAsync(
                FilterDefinition<AppliedMigration>.Empty, null, cancellationToken);
            await applied.ForEachAsync(x => appliedMigrations.Add(x.Version),
                cancellationToken);

            return allMigrations.Where(x => !appliedMigrations.Contains(x.Version)); // still ordered
        }

		public virtual async Task<bool> IsNotUpToDateAsync(
            CancellationToken cancellationToken = default(CancellationToken))
		{
            return (await GetUnappliedMigrationsAsync(cancellationToken)).Any();
		}

		public virtual async Task ThrowIfNotUpToDateAsync(
            CancellationToken cancellationToken = default(CancellationToken))
		{
            var unapplied = (await GetUnappliedMigrationsAsync(
                cancellationToken)).FirstOrDefault();
            if (unapplied == null)
			{
				return;
			}
            throw new ApplicationException(
                $"Database contains unapplied migrations starting with {unapplied.Version}");
		}

		public virtual async Task<MigrationVersion> GetVersionAsync(
            CancellationToken cancellationToken = default(CancellationToken))
		{
			var lastAppliedMigration = await GetLastAppliedMigrationAsync(cancellationToken);
			return lastAppliedMigration == null
			       	? MigrationVersion.Default
			       	: lastAppliedMigration.Version;
		}

        public virtual async Task<AppliedMigration> GetLastAppliedMigrationAsync(
            CancellationToken cancellationToken = default(CancellationToken))
		{
            // Fetch the lot since we need to sort by the parsed version number
            // (serialize it as 3 ints??)
            var migrations = await GetMigrationsApplied().FindAsync(
                FilterDefinition<AppliedMigration>.Empty, null, cancellationToken);
            var migrationsList = await migrations
                .ToListAsync(cancellationToken);
            return migrationsList
                .OrderByDescending(v => v.Version)
                .FirstOrDefault();
		}

		public virtual async Task<AppliedMigration> StartMigrationAsync(Migration migration, 
            CancellationToken cancellationToken = default(CancellationToken))
		{
			var appliedMigration = new AppliedMigration(migration);
			await GetMigrationsApplied().InsertOneAsync(
                appliedMigration, null, cancellationToken);
			return appliedMigration;
		}

		public virtual Task CompleteMigrationAsync(AppliedMigration appliedMigration,
            CancellationToken cancellationToken = default(CancellationToken))
		{
            return GetMigrationsApplied().UpdateOneAsync(
                x => x.Version == appliedMigration.Version,
                Builders<AppliedMigration>.Update.Set(x => x.CompletedOn, DateTime.UtcNow),
                null, cancellationToken);
		}

		public virtual async Task MarkUpToVersionAsync(MigrationVersion version,
            CancellationToken cancellationToken = default(CancellationToken))
		{
            var migrations = _Runner.MigrationLocator
                .GetAllMigrations()
                .Where(m => m.Version <= version);
            foreach (var migration in migrations)
            {
                await MarkVersionAsync(migration.Version, cancellationToken);
            }
		}

		public virtual Task MarkVersionAsync(MigrationVersion version,
            CancellationToken cancellationToken = default(CancellationToken))
		{
			var appliedMigration = AppliedMigration.MarkerOnly(version);
			return GetMigrationsApplied().InsertOneAsync(appliedMigration,
                null, cancellationToken);
		}
	}
}
