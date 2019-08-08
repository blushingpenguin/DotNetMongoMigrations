using MongoDB.Bson;
using MongoDB.Driver;
using System.Threading;
using System.Threading.Tasks;

namespace MongoMigrations.Test
{
    class M20190718163800_TestMigration : Migration
    {
        public M20190718163800_TestMigration() :
            base("A test migration")
        {
        }

        public override async Task UpdateAsync(
            CancellationToken cancellationToken)
        {
            var collection = Database.GetCollection<BsonDocument>("TestDocs");
            await collection.UpdateManyAsync(
                FilterDefinition<BsonDocument>.Empty,
                Builders<BsonDocument>.Update.Set("NewField", 1));
        }
    }
}
