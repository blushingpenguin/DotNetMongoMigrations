using MongoDB.Bson;
using MongoDB.Driver;
using System.Threading.Tasks;

namespace MongoMigrations.Test
{
    class TestMigration : Migration
    {
        public TestMigration() :
            base("1.0.0")
        {
            Description = "A test migration";
        }

        public override async Task UpdateAsync()
        {
            var collection = Database.GetCollection<BsonDocument>("TestDocs");
            await collection.UpdateManyAsync(
                FilterDefinition<BsonDocument>.Empty,
                Builders<BsonDocument>.Update.Set("NewField", 1));
        }
    }
}
