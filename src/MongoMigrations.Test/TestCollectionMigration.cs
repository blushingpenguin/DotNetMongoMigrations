using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Threading.Tasks;

namespace MongoMigrations.Test
{
    class TestCollectionMigration : CollectionMigration
    {
        public bool Throw { get; set; }

        public TestCollectionMigration() :
            this(false)
        {
        }

        public TestCollectionMigration(bool throw_) :
            base("1.0.1", "TestDocs")
        {
            Throw = throw_;
            Description = "A test collection migration";
        }

        public override Task<bool> UpdateDocumentAsync(
            IMongoCollection<BsonDocument> collection, BsonDocument document)
        {
            if (Throw)
            {
                throw new Exception("Update failure");
            }
            document.Set("AnotherNewField", "bob");
            return Task.FromResult(true);
        }
    }
}
