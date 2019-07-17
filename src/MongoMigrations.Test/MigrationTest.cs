using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using System.Threading.Tasks;

namespace MongoMigrations.Test
{
    class MigrationMocks
    {
        public IMongoClient Client { get; set; }
        public IMongoDatabase Db { get; set; }
        public MigrationRunner Runner { get; set; }
    }

    class TestMigrationLocator : MigrationLocator
    {
        public List<Migration> Migrations { get; } = new List<Migration>();

        public override IEnumerable<Migration> GetAllMigrations() =>
            Migrations;
    }

    [Parallelizable(ParallelScope.All)]
    public class MigrationTest
    {
        private async Task<MigrationMocks> CreateMocksAsync(string dbName)
        {
            var mocks = new MigrationMocks();
            mocks.Client = new MongoClient("mongodb://localhost:27017/");
            await mocks.Client.DropDatabaseAsync(dbName);
            mocks.Db = mocks.Client.GetDatabase(dbName);
            mocks.Runner = new MigrationRunner(mocks.Client, dbName);
            return mocks;
        }
        
        [Test]
        public async Task RunMigration()
        {
            string dbName = "fdeb1aa2-7a5e-4cae-9616-3e58613e0eab";
            var mocks = await CreateMocksAsync(dbName);

            var collection = mocks.Db.GetCollection<BsonDocument>("TestDocs");
            await collection.InsertManyAsync(new[]
            {
                new BsonDocument((IEnumerable<BsonElement>)new [] {
                    new BsonElement("_id", new BsonObjectId(new ObjectId("5d2dbdd31f326a50ac81b9b3"))),
                    new BsonElement("ExistingField", "foo"),
                    new BsonElement("ANumberField", 10)
                })
            });

            mocks.Runner.MigrationLocator.MigrationFilters.Clear(); // keep experimental
            mocks.Runner.MigrationLocator.LookForMigrationsInAssemblyOfType<MigrationTest>();
            mocks.Runner.MigrationLocator.LookForMigrationsInAssembly(Assembly.GetExecutingAssembly());
            await mocks.Runner.UpdateToLatestAsync();

            var result = await collection.Find(
                FilterDefinition<BsonDocument>.Empty).ToListAsync();
            result.Should().BeEquivalentTo(
                new[]
            {
                new BsonDocument((IEnumerable<BsonElement>)new [] {
                    new BsonElement("_id", new BsonObjectId(new ObjectId("5d2dbdd31f326a50ac81b9b3"))),
                    new BsonElement("ExistingField", "foo"),
                    new BsonElement("ANumberField", 10),
                    new BsonElement("NewField", 1),
                    new BsonElement("AnotherNewField", "bob")
                })
            });
            await mocks.Client.DropDatabaseAsync(dbName);
            await mocks.Client.DropDatabaseAsync(dbName + "_MigrationBackup");
        }

        [Test]
        public async Task RunMigrationWithFailure()
        {
            string dbName = "3b953cb1-5875-4b22-966e-66451075bd56";
            var mocks = await CreateMocksAsync(dbName);
            var locator = new TestMigrationLocator();
            locator.Migrations.Add(new TestCollectionMigration(true));
            mocks.Runner.MigrationLocator = locator;

            var collection = mocks.Db.GetCollection<BsonDocument>("TestDocs");
            await collection.InsertManyAsync(new[]
            {
                new BsonDocument((IEnumerable<BsonElement>)new [] {
                    new BsonElement("_id", new BsonObjectId(new ObjectId("5d2dbdd31f326a50ac81b9b3"))),
                    new BsonElement("ExistingField", "foo"),
                })
            });

            Func<Task> a = () => mocks.Runner.UpdateToLatestAsync();
            (await a.Should().ThrowAsync<MigrationException>())
                .WithMessage("{ Message = Migration failed to be applied: { Message = Failed to update document*");
            await mocks.Client.DropDatabaseAsync(dbName);
            await mocks.Client.DropDatabaseAsync(dbName + "_MigrationBackup");
        }
    }
}
