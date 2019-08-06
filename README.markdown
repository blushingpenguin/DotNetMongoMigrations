[![ci.appveyor.com](https://ci.appveyor.com/api/projects/status/github/blushingpenguin/DotNetMongoMigrations?branch=master&svg=true)](https://ci.appveyor.com/api/projects/status/github/blushingpenguin/DotNetMongoMigrations?branch=master&svg=true)
[![codecov.io](https://codecov.io/gh/blushingpenguin/DotNetMongoMigrations/coverage.svg?branch=master)](https://codecov.io/gh/blushingpenguin/DotNetMongoMigrations?branch=master)

Why mongo migrations?
--
We no longer need create schema migrations, as this is a schemaless database, when we add collections (tables) or properties on our entities (columns), we don't need to run creation scripts.

However, we need migrations when:

1. Renaming collections
1. Renaming keys
1. Manipulating data types, e.g. moving data between types and setting default values for new properties
1. Index creation
1. Removing collections / data

So the idea is to have a simple migration script that executes commands against Mongo.  It should track the applied migrations and quickly be able to apply new migrations.

Why DotNetMongoMigrations?
--

1. This project is meant to allow for migrations to be created in C# or other .Net languages and kept with the code base.
1. Access .Net APIs for manipulating documents
1. Leverage existing NUnit test projects or other test projects to test migrations
1. Provide an automatable foundation to track and apply migrations.
1. Make development easy by automatically backing up and restoring the database state.

Migration recommendations
--

1. Keep them as simple as possible
1. Do not couple migrations to your domain types, they will be brittle to change, and the point of a migration is to update the data representation when your model changes.
1. Stick to the mongo BsonDocument interface or use javascript based mongo commands for migrations, much like with SQL, the mongo javascript API is less likely to change which might break migrations
1. Add an application startup check that the database is at the correct version
1. Write tests of your migrations, TDD them from existing data scenarios to new forms
1. Automate the deployment of migrations

Versions
--

Migration are versioned by timestamp. This is preferred to a serial number to avoid merge conflicts. To ensure that version numbers are applied migration classes must be named MyyyyMMddHHmmss_Description, for example M20190806073226_AddANotNullProperty. Migrations are applied in timestamp order.

Migration
--

This is a simple migration that adds a new property to a collection:

```csharp
	public class M20190806072600_AddNewField : Migration
	{
		public override async Task UpdateAsync()
		{
            var collection = Database.GetCollection<BsonDocument>("TestDocs");
            await collection.UpdateManyAsync(
                FilterDefinition<BsonDocument>.Empty,
                Builders<BsonDocument>.Update.Set("NewField", 1));
		}
	}
```

Collection Migration
--

These are migrations performed on every document in a given collection.  Supply the version number and collection name, then simply implement the update per document method UpdateDocumentAsync to manipulate each document.  If the document should be replaced, then return true and ReplaceOneAsync will be called using the _id property of the document as a filter.

```csharp
	public class M20190806073300_DropSocialSecurityInfo : CollectionMigration
	{
		protected override Task<bool> UpdateDocumentAsync(MongoCollection<BsonDocument> collection, BsonDocument document)
		{
			document.Remove("SocialSecurityNumber");
			return Task.FromResult(true);
		}
	}
```

Experimental Migrations
--

Sometimes we want to work on a migration but it's not complete yet, these can be attributed with the ExperimentalAttribute and the base migrations runner will exclude them.

If any experimental migrations are applied then the migration runner will clone all the collections in the database into a database named Database_MigrationBackup. If one of these is found on the next run, then the database will be restored before migrations are applied.  Restoring the database also restores the history of applied versions, so this means that you can test your migration simply by repeatedly running it during development.

```csharp
	[Experimental]
	public class M20190806073400_MarmaliseData : Migration
	{
```

Running Migrations
--

The project provides a MigrationRunner which contains:

* DatabaseMigrationStatus
 * Contains methods to monitor the version of the database.
 * Applied migrations are stored in a collection named "DatabaseVersion", this can be configured via the VersionCollectionName instance property.
* MigrationLocator
 * Scans the provided assemblies for migrations
 * Filters on experimental by default.
 * Filters can be added/removed via the MigrationLocator.MigrationFilters list.

Sample App Startup Version Check
--

Simply plug the following code into your application startup, whether that be a web or service app, to terminate the application if the database is not at the expected version.  This assumes you have mongo server location and database name in a settings file and that Migration1 is in the assembly containing migrations to be scanned for.

```csharp
	public class CheckDbVersionOnStartup : IRunOnApplicationStart
	{
		public void Start()
		{
			var runner = new MigrationRunner(Settings.Default.MongoServerLocation, Settings.Default.MongoDatabaseName);
			runner.MigrationLocator.LookForMigrationsInAssemblyOfType<Migration1>();
			runner.DatabaseStatus.ThrowIfNotLatestVersion();
		}
	}
```

Using migrations with a deployment process
--

Document databases for the most do not support transactions with rollback, therefore it's a good idea to backup a database before applying migrations.  Also, in order to reduce the manual work involved and the risk of error, it's a good idea to automate your migration deployments.  Mongo supports a backup and restore utility out of the box, this can be combined with the migrations above to provide an automated deployment of the migrations.

Here is a sample powershell script we use to automate this deployment

[Migration PowerShell Script](https://github.com/phoenixwebgroup/DotNetMongoMigrations/blob/master/MigrateMongo.ps1)

It takes parameters for server name/ip, database name, base backup directory and migration dll path

We execute it from our rake deployment via:

```ruby
sh 'powershell -ExecutionPolicy Unrestricted -File deployments\mongo\MigrateMongo.ps1 ' + host + ' databaseName deployments\mongo\backup build\Migrations.dll '
```

Testing migrations
--

Here is a sample test to rename a key on a document via the BsonDocument api, obviously this is a trivial case but you can see how you can leverage manipulations of the BsonDocument api to test your migrations.  Also, you could create migration databases and prepopulate them with sample data when using the mongo json api.

```csharp
	[Test]
	public void MigrationToRenameNameToFullName_HasNameKey_RenamesToFullName()
	{
		var nameKey = "Name";
		var nameValue = "Bob";
		var document = new BsonDocument {{nameKey, nameValue}};
		var migration = new M20190806073514_RenameNameToFullName();

		migration.Rename(document);

		Expect(document.Contains(nameKey), Is.False);
		var fullNameKey = "FullName";
		Expect(document[fullNameKey].AsString, Is.EqualTo(nameValue));
	}

	public class M20190806073514_RenameNameToFullName : CollectionMigration
	{
		public override Task<bool> UpdateDocumentAsync(MongoCollection<BsonDocument> collection, BsonDocument document)
		{
			Rename(document);
			return Task.FromResult(true);
		}

		public void Rename(BsonDocument document)
		{
			document.Add("FullName", document["Name"]);
			document.Remove("Name");
		}
	}
```

Port
--
This code is based on the original implementation located at https://github.com/phoenixwebgroup/DotNetMongoMigrations. It has been ported to .NET standard to allow use from both .NET core and the .NET framework, the automatic backup facility has been added, the code has been updated to the latest mongo driver, tests have been created and the migration version number format has been changed to timestamps (from serial numbers).
