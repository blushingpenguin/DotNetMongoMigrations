namespace MongoMigrations
{
	using MongoDB.Driver;
    using System.Threading.Tasks;

    public abstract class Migration
	{
		public string Description { get; protected set; }

        protected Migration(string description = null)
        {
            Description = description;
            Version = new MigrationVersion(GetType().Name);
        }

        public MigrationVersion Version { get; }

		public IMongoDatabase Database { get; set; }

		public abstract Task UpdateAsync();
	}
}