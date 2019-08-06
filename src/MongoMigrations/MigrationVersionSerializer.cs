namespace MongoMigrations
{
    using MongoDB.Bson.Serialization;
    using MongoDB.Bson.Serialization.Serializers;

    public class MigrationVersionSerializer : SerializerBase<MigrationVersion>
	{
	    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, MigrationVersion value)
	    {
            context.Writer.WriteString(value.ToString());
	    }

	    public override MigrationVersion Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
	    {
            var versionString = context.Reader.ReadString();
            return new MigrationVersion(versionString);
	    }
	}
}