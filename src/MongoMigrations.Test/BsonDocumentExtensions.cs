using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using FluentAssertions;
using MongoDB.Bson;

namespace MongoMigrations.Test
{
    [Parallelizable(ParallelScope.All)]
    public class BsonDocumentExtensions
    {
        [Test]
        public void TryGetDocumentIdWithId()
        {
            var doc = new BsonDocument();
            doc.Add("_id", new BsonObjectId(new ObjectId("5d2dbdd31f326a50ac81b9b3")));
            var result = doc.TryGetDocumentId();
            result.Should().BeOfType<BsonObjectId>();
            result.Should().BeEquivalentTo(new BsonObjectId(new ObjectId("5d2dbdd31f326a50ac81b9b3")));
        }

        [Test]
        public void TryGetDocumentIdWithoutId()
        {
            var doc = new BsonDocument();
            var result = doc.TryGetDocumentId();
            result.Should().BeOfType<BsonString>();
            result.Should().BeEquivalentTo(new BsonString("Cannot find id")); // do we really want this?!
        }

        [Test]
        public void ChangeName()
        {
            var doc = new BsonDocument();
            doc.Add("old", "SPG");
            doc.Add("untouched", "leave it");
            doc.ChangeName("old", "new");
            doc.Should().BeEquivalentTo(
                new BsonDocument((IEnumerable<BsonElement>)new[]
                {
                    new BsonElement("new", "SPG"),
                    new BsonElement("untouched", "leave it")
                })
            );
        }
    }
}
