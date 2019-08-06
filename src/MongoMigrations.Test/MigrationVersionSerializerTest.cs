using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using NUnit.Framework;
using System.IO;

namespace MongoMigrations.Test
{
    [Parallelizable(ParallelScope.All)]
    public class MigrationVersionSerializerTest
    {
        [Test]
        public void RoundTrip()
        {
            using (var ms = new MemoryStream())
            {
                using (var writer = new BsonBinaryWriter(ms))
                {
                    var bsContext = BsonSerializationContext.CreateRoot(writer);
                    var serializer = new MigrationVersionSerializer();
                    var bsArgs = new BsonSerializationArgs();

                    writer.WriteStartDocument();
                    writer.WriteName("version");
                    serializer.Serialize(bsContext, bsArgs, new MigrationVersion("M20010203040506_foo"));
                    writer.WriteEndDocument();
                }
                ms.Position = 0;
                using (var reader = new BsonBinaryReader(ms))
                {
                    var bsContext = BsonDeserializationContext.CreateRoot(reader);
                    var serializer = new MigrationVersionSerializer();
                    var bsArgs = new BsonDeserializationArgs();

                    reader.ReadStartDocument();
                    reader.ReadName().Should().Be("version");
                    var version = serializer.Deserialize(bsContext, bsArgs);
                    version.Should().BeEquivalentTo(new MigrationVersion("M20010203040506_foo"));
                    reader.ReadEndDocument();
                }
            }
        }
    }
}
