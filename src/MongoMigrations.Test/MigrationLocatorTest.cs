using NUnit.Framework;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using System.Reflection;
using System.Linq;

namespace MongoMigrations.Test
{
    public class MigrationLocatorTest
    {
        [Test]
        public void GetMigrationsAfterNull()
        {
            var locator = new MigrationLocator();
            locator.LookForMigrationsInAssembly(Assembly.GetExecutingAssembly());
            var migrations = locator.GetMigrationsAfter(null);
            // check migrations are ordered
            migrations.Select(x => x.Version).Should().BeEquivalentTo(new[]
            {
                new MigrationVersion("M20190718160124_WithoutAttributeMigration"),
                new MigrationVersion("M20190718162300_TestCollectionMigration"),
                new MigrationVersion("M20190718163800_TestMigration")
            }, opts => opts.WithStrictOrdering());
        }

        [Test]
        public void GetMigrationsAfterVersion()
        {
            var locator = new MigrationLocator();
            locator.LookForMigrationsInAssembly(Assembly.GetExecutingAssembly());
            var migrations = locator.GetMigrationsAfter(
                new AppliedMigration(new M20190718162300_TestCollectionMigration()));
            // check migrations are ordered
            migrations.Select(x => x.Version).Should().BeEquivalentTo(new[]
            {
                new MigrationVersion("M20190718163800_TestMigration")
            });
        }

        [Test]
        public void GetLatestVersionNoMigrations()
        {
            var locator = new MigrationLocator();
            locator.LatestVersion().Should().BeEquivalentTo(
                MigrationVersion.Default);
        }

        [Test]
        public void GetLatestVersionWithMigrations()
        {
            var locator = new MigrationLocator();
            locator.LookForMigrationsInAssembly(Assembly.GetExecutingAssembly());
            locator.LatestVersion().Should().BeEquivalentTo(
                new MigrationVersion(new DateTime(2019, 7, 18, 16, 38, 00), "TestMigration"));
        }

        [Test]
        public void HandlesMigrationFilterException()
        {
            var locator = new MigrationLocator();
            var badAsm = Substitute.For<Assembly>();
            badAsm.GetTypes().Returns(x => throw new Exception("baadf00d"));
            locator.LookForMigrationsInAssembly(badAsm);
            locator.Invoking(l => l.GetAllMigrations().ToArray()).Should()
                .Throw<MigrationException>()
                .WithMessage("Cannot load migrations from assembly*");
        }
    }
}
