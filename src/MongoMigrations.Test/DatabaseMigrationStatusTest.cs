using FluentAssertions;
using MongoDB.Driver;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MongoMigrations.Test
{
    class DatabaseMigrationStatusMocks
    {
        public DatabaseMigrationStatus MigrationStatus { get; set; }
        public MigrationRunner Runner { get; set; }
        public IMongoClient Client { get; set; }
        public IMongoDatabase Db { get; set; }

        public DatabaseMigrationStatusMocks(string dbName)
        {
            Client = new MongoClient("mongodb://localhost:27017/");
            Runner = new MigrationRunner(Client, dbName);
            MigrationStatus = new DatabaseMigrationStatus(Runner);
        }
    }

    [Parallelizable(ParallelScope.All)]
    public class DatabaseMigrationStatusTest
    {
        private async Task<DatabaseMigrationStatusMocks> CreateMocksAsync(string dbName)
        {
            var mocks = new DatabaseMigrationStatusMocks(dbName);
            await mocks.Client.DropDatabaseAsync(dbName);
            mocks.Db = mocks.Runner.Client.GetDatabase(mocks.Runner.DatabaseName);
            return mocks;
        }

        [Test]
        public async Task GetMigrationsApplied()
        {
            string dbName = "ea2f5ece-3a9c-454c-ad6e-2da1483435b6";
            var mocks = await CreateMocksAsync(dbName);
            var applied = mocks.MigrationStatus.GetMigrationsApplied();
            applied.CollectionNamespace.CollectionName.Should().Be("DatabaseVersion");
            await mocks.Client.DropDatabaseAsync(dbName);
        }

        [Test]
        public async Task GetLastAppliedMigrationAsync()
        {
            string dbName = "6808a61f-446c-4b10-8090-c58dd35a10fc";
            var mocks = await CreateMocksAsync(dbName);
            var applied = mocks.MigrationStatus.GetMigrationsApplied();
            await applied.InsertManyAsync(new[]
            {
                new AppliedMigration(new TestMigration()),
                new AppliedMigration(new TestCollectionMigration()),
            });
            var lastAm = await mocks.MigrationStatus.GetLastAppliedMigrationAsync();
            lastAm.Description.Should().Be("A test collection migration");
            await mocks.Client.DropDatabaseAsync(dbName);
        }

        [Test]
        public async Task GetVersionEmptyDb()
        {
            string dbName = "fa72f8df-4b49-4f58-a89c-281d44c541e3";
            var mocks = await CreateMocksAsync(dbName);
            var result = await mocks.MigrationStatus.GetVersionAsync();
            result.Should().BeEquivalentTo(MigrationVersion.Default());
        }

        [Test]
        public async Task GetVersion_1_0_0()
        {
            string dbName = "ba0ffc68-9b84-4dde-8c93-a05c0ae3946d";
            var mocks = await CreateMocksAsync(dbName);
            var applied = mocks.MigrationStatus.GetMigrationsApplied();
            await applied.InsertOneAsync(
                new AppliedMigration(new TestMigration()));
            var result = await mocks.MigrationStatus.GetVersionAsync();
            result.Should().BeEquivalentTo(new MigrationVersion(1, 0, 0));
            await mocks.Client.DropDatabaseAsync(dbName);
        }

        [Test]
        public async Task IsNotLatestVersionFalse()
        {
            string dbName = "3d772e5f-fe5f-47ec-9238-eaf30313b67a";
            var mocks = await CreateMocksAsync(dbName);
            mocks.Runner.MigrationLocator.LookForMigrationsInAssembly(
                Assembly.GetExecutingAssembly());
            var applied = mocks.MigrationStatus.GetMigrationsApplied();
            await applied.InsertOneAsync(
                new AppliedMigration(new WithoutAttributeMigration()));
            var result = await mocks.MigrationStatus.IsNotLatestVersionAsync();
            result.Should().BeFalse();
            await mocks.Client.DropDatabaseAsync(dbName);
        }

        [Test]
        public async Task IsNotLatestVersionTrue()
        {
            string dbName = "3ffd4dac-032b-4064-bd84-beffc922742d";
            var mocks = await CreateMocksAsync(dbName);
            mocks.Runner.MigrationLocator.LookForMigrationsInAssembly(
                Assembly.GetExecutingAssembly());
            var result = await mocks.MigrationStatus.IsNotLatestVersionAsync();
            result.Should().BeTrue();
            await mocks.Client.DropDatabaseAsync(dbName);
        }

        [Test]
        public async Task ThrowIfNotLatestVersionFalse()
        {
            string dbName = "f13feac6-47d6-4741-9e57-00d56d94c35c";
            var mocks = await CreateMocksAsync(dbName);
            mocks.Runner.MigrationLocator.LookForMigrationsInAssembly(
                Assembly.GetExecutingAssembly());
            var applied = mocks.MigrationStatus.GetMigrationsApplied();
            await applied.InsertOneAsync(
                new AppliedMigration(new WithoutAttributeMigration()));
            Func<Task> a = () => mocks.MigrationStatus.ThrowIfNotLatestVersionAsync();
            await a.Should().NotThrowAsync();
            await mocks.Client.DropDatabaseAsync(dbName);
        }

        [Test]
        public async Task ThrowIfNotLatestVersionTrue()
        {
            string dbName = "632eb793-d71c-4fb5-a236-20d44c6d99cd";
            var mocks = await CreateMocksAsync(dbName);
            mocks.Runner.MigrationLocator.LookForMigrationsInAssembly(
                Assembly.GetExecutingAssembly());
            Func<Task> a = () => mocks.MigrationStatus.ThrowIfNotLatestVersionAsync();
            (await a.Should().ThrowAsync<ApplicationException>())
                .And.Message.Should().StartWith("Database is not the expected version");
            await mocks.Client.DropDatabaseAsync(dbName);
        }

        [Test]
        public async Task MarkUpToVersion()
        {
            string dbName = "3b5776ad-0431-4911-bc06-a8192bd5431e";
            var mocks = await CreateMocksAsync(dbName);
            mocks.Runner.MigrationLocator.LookForMigrationsInAssembly(
                Assembly.GetExecutingAssembly());
            await mocks.MigrationStatus.MarkUpToVersionAsync(new MigrationVersion("1.1.1"));
            var applied = await mocks.MigrationStatus.GetMigrationsApplied()
                .Find(FilterDefinition<AppliedMigration>.Empty)
                .ToListAsync();
            applied.Should().BeEquivalentTo(new[]
            {
                new AppliedMigration(new TestMigration()),
                new AppliedMigration(new TestCollectionMigration())
            }, opts => opts
                .Excluding(x => x.StartedOn)
                .Excluding(x => x.CompletedOn)
                .Excluding(x => x.Description) // gets set to "manually marked"
            );
            await mocks.Client.DropDatabaseAsync(dbName);
        }

        [Test]
        public async Task MarkVersion()
        {
            string dbName = "24aabaf9-9bb2-4feb-afaa-3d4a2ac2f0b8";
            var mocks = await CreateMocksAsync(dbName);
            await mocks.MigrationStatus.MarkVersionAsync(new MigrationVersion("3.4.5"));
            var am = await mocks.MigrationStatus.GetMigrationsApplied()
                    .Find(FilterDefinition<AppliedMigration>.Empty).SingleAsync();
            am.Version.Should().BeEquivalentTo(new MigrationVersion("3.4.5"));
            await mocks.Client.DropDatabaseAsync(dbName);
        }
    }
}
