namespace MongoMigrations
{
    using MongoDB.Bson;
    using MongoDB.Driver;
    using System;
    using System.Threading.Tasks;

    public abstract class CollectionMigration : Migration
    {
        protected string CollectionName;

        public CollectionMigration(MigrationVersion version, string collectionName) : base(version)
        {
            CollectionName = collectionName;
        }

        public virtual FilterDefinition<BsonDocument> Filter()
        {
            return null;
        }

        public override async Task UpdateAsync()
        {
            var collection = GetCollection();
            var documents = await GetDocumentsAsync(collection);
            await UpdateDocumentsAsync(collection, documents);
        }

        public virtual Task UpdateDocumentsAsync(IMongoCollection<BsonDocument> collection, IAsyncCursor<BsonDocument> documents)
        {
            return documents.ForEachAsync(async document =>
            {
                try
                {
                    if (await UpdateDocumentAsync(collection, document))
                    {
                        var result = await collection.ReplaceOneAsync(
                            Builders<BsonDocument>.Filter.Eq("_id", document.GetElement("_id").Value),
                            document);
                        if (result.MatchedCount != 1)
                        {
                            throw new InvalidOperationException(
                                $"Failed to update the document with id {document.TryGetDocumentId()}");
                        }
                    }
                }
                catch (Exception exception)
                {
                    OnErrorUpdatingDocument(document, exception);
                }
            });
        }

        protected virtual void OnErrorUpdatingDocument(BsonDocument document, Exception exception)
        {
            var message =
                new
                    {
                        Message = "Failed to update document",
                        CollectionName,
                        Id = document.TryGetDocumentId(),
                        MigrationVersion = Version,
                        MigrationDescription = Description
                    };
            throw new MigrationException(message.ToString(), exception);
        }

        public abstract Task<bool> UpdateDocumentAsync(IMongoCollection<BsonDocument> collection, BsonDocument document);

        protected virtual IMongoCollection<BsonDocument> GetCollection()
        {
            return Database.GetCollection<BsonDocument>(CollectionName);
        }

        protected virtual Task<IAsyncCursor<BsonDocument>> GetDocumentsAsync(IMongoCollection<BsonDocument> collection)
        {
            var query = Filter() ?? FilterDefinition<BsonDocument>.Empty;
            return collection.Find(query).ToCursorAsync();
        }
    }
}
