using FluentAssertions;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public DatabaseMigrationStatusMocks(string dbName, 
            ILogger<MigrationRunner> logger = null)
        {
            Client = new MongoClient(Settings.DatabaseConnectionString);
            Runner = new MigrationRunner(Client, dbName, logger);
            MigrationStatus = new DatabaseMigrationStatus(Runner);
        }
    }

    [Parallelizable(ParallelScope.All)]
    public class DatabaseMigrationStatusTest
    {
        private async Task<DatabaseMigrationStatusMocks> CreateMocksAsync(
            string dbName, ILogger<MigrationRunner> logger = null)
        {
            var mocks = new DatabaseMigrationStatusMocks(dbName, logger);
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
                new AppliedMigration(new M20190718163800_TestMigration()),
                new AppliedMigration(new M20190718162300_TestCollectionMigration()),
            });
            var lastAm = await mocks.MigrationStatus.GetLastAppliedMigrationAsync();
            lastAm.Description.Should().Be("A test migration");
            await mocks.Client.DropDatabaseAsync(dbName);
        }

        [Test]
        public async Task GetVersionEmptyDb()
        {
            string dbName = "fa72f8df-4b49-4f58-a89c-281d44c541e3";
            var mocks = await CreateMocksAsync(dbName);
            var result = await mocks.MigrationStatus.GetVersionAsync();
            result.Should().BeEquivalentTo(MigrationVersion.Default);
        }

        [Test]
        public async Task GetVersion_M20190718163800_TestMigration()
        {
            string dbName = "ba0ffc68-9b84-4dde-8c93-a05c0ae3946d";
            var mocks = await CreateMocksAsync(dbName);
            var applied = mocks.MigrationStatus.GetMigrationsApplied();
            await applied.InsertOneAsync(
                new AppliedMigration(new M20190718163800_TestMigration()));
            var result = await mocks.MigrationStatus.GetVersionAsync();
            result.Should().BeEquivalentTo(new MigrationVersion(
                new DateTime(2019, 7, 18, 16, 38, 0), "TestMigration"));
            await mocks.Client.DropDatabaseAsync(dbName);
        }

        [Test]
        public async Task IsNotUpToDateAsyncFalse()
        {
            string dbName = "3d772e5f-fe5f-47ec-9238-eaf30313b67a";
            var mocks = await CreateMocksAsync(dbName);
            mocks.Runner.MigrationLocator.LookForMigrationsInAssembly(
                Assembly.GetExecutingAssembly());
            var applied = mocks.MigrationStatus.GetMigrationsApplied();
            await applied.InsertManyAsync(new[] {
                new AppliedMigration(new M20190718160124_WithoutAttributeMigration()),
                new AppliedMigration(new M20190718162300_TestCollectionMigration()),
                new AppliedMigration(new M20190718163800_TestMigration())
            });
            var result = await mocks.MigrationStatus.IsNotUpToDateAsync();
            result.Should().BeFalse();
            await mocks.Client.DropDatabaseAsync(dbName);
        }

        [Test]
        public async Task IsNotUpToDateAsyncTrue()
        {
            string dbName = "3ffd4dac-032b-4064-bd84-beffc922742d";
            var mocks = await CreateMocksAsync(dbName);
            mocks.Runner.MigrationLocator.LookForMigrationsInAssembly(
                Assembly.GetExecutingAssembly());
            var result = await mocks.MigrationStatus.IsNotUpToDateAsync();
            result.Should().BeTrue();
            await mocks.Client.DropDatabaseAsync(dbName);
        }

        [Test]
        public async Task ThrowIfNotUpToDateFalse()
        {
            string dbName = "f13feac6-47d6-4741-9e57-00d56d94c35c";
            var mocks = await CreateMocksAsync(dbName);
            mocks.Runner.MigrationLocator.LookForMigrationsInAssembly(
                Assembly.GetExecutingAssembly());
            var applied = mocks.MigrationStatus.GetMigrationsApplied();
            await applied.InsertManyAsync(new[] {
                new AppliedMigration(new M20190718160124_WithoutAttributeMigration()),
                new AppliedMigration(new M20190718162300_TestCollectionMigration()),
                new AppliedMigration(new M20190718163800_TestMigration())
            });
            Func<Task> a = () => mocks.MigrationStatus.ThrowIfNotUpToDateAsync();
            await a.Should().NotThrowAsync();
            await mocks.Client.DropDatabaseAsync(dbName);
        }

        [Test]
        public async Task ThrowIfNotUpToDateTrue()
        {
            string dbName = "632eb793-d71c-4fb5-a236-20d44c6d99cd";
            var mocks = await CreateMocksAsync(dbName);
            mocks.Runner.MigrationLocator.LookForMigrationsInAssembly(
                Assembly.GetExecutingAssembly());
            Func<Task> a = () => mocks.MigrationStatus.ThrowIfNotUpToDateAsync();
            (await a.Should().ThrowAsync<ApplicationException>())
                .And.Message.Should().Be("Database contains unapplied migrations starting with M20190718160124_WithoutAttributeMigration");
            await mocks.Client.DropDatabaseAsync(dbName);
        }

        [Test]
        public async Task MarkUpToVersion()
        {
            string dbName = "3b5776ad-0431-4911-bc06-a8192bd5431e";
            var mocks = await CreateMocksAsync(dbName);
            mocks.Runner.MigrationLocator.LookForMigrationsInAssembly(
                Assembly.GetExecutingAssembly());
            await mocks.MigrationStatus.MarkUpToVersionAsync(new MigrationVersion("M20190718163700_marker"));
            var applied = await mocks.MigrationStatus.GetMigrationsApplied()
                .Find(FilterDefinition<AppliedMigration>.Empty)
                .ToListAsync();
            applied.Should().BeEquivalentTo(new[]
            {
                new AppliedMigration(new M20190718162300_TestCollectionMigration()),
                new AppliedMigration(new M20190718160124_WithoutAttributeMigration())
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
            await mocks.MigrationStatus.MarkVersionAsync(new MigrationVersion("M20010203040506_test"));
            var am = await mocks.MigrationStatus.GetMigrationsApplied()
                    .Find(FilterDefinition<AppliedMigration>.Empty).SingleAsync();
            am.Version.Should().BeEquivalentTo(new MigrationVersion(
                new DateTime(2001, 2, 3, 4, 5,6 ), "test"));
            await mocks.Client.DropDatabaseAsync(dbName);
        }

        [Test]
        public async Task UsesSuppliedLogger()
        {
            string dbName = "4e975090-7e9a-44b4-8d31-f0e5204b3f30";

            var logger = Substitute.For<ILogger<MigrationRunner>>();
            var mocks = await CreateMocksAsync(dbName, logger);
            mocks.Runner.MigrationLocator.LookForMigrationsInAssembly(
                Assembly.GetExecutingAssembly());
            await mocks.Runner.UpdateToLatestAsync();
            var calls = logger.ReceivedCalls().ToArray();
            calls.Length.Should().Be(5);
            calls[0].GetArguments()[2].ToString().Should().BeEquivalentTo(
                "Updating server(s) \"localhost\" for database " +
                "\"4e975090-7e9a-44b4-8d31-f0e5204b3f30\" to latest...");
            calls[4].GetArguments()[2].ToString().Should().BeEquivalentTo(
                "{ Message = Applying migration, Version = M20190718163800_TestMigration, " +
                "Description = A test migration, DatabaseName = 4e975090-7e9a-44b4-8d31-f0e5204b3f30 }");
            await mocks.Client.DropDatabaseAsync(dbName);
        }
    }
}
