using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MongoMigrations.Test
{
    class M20190718162300_TestCollectionMigration : CollectionMigration
    {
        public bool Throw { get; set; }

        public M20190718162300_TestCollectionMigration() :
            this(false)
        {
        }

        public M20190718162300_TestCollectionMigration(bool throw_) :
            base("TestDocs", "A test collection migration")
        {
            Throw = throw_;
        }

        public override Task<bool> UpdateDocumentAsync(
            IMongoCollection<BsonDocument> collection,
            BsonDocument document,
            CancellationToken cancellationToken)
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
