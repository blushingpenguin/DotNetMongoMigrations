using FluentAssertions;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace MongoMigrations.Test
{
    [Experimental]
    public class WithAttributeMigration : Migration
    {
        public WithAttributeMigration() :
            base("1.2.3")
        {
        }

        public override Task UpdateAsync() => Task.CompletedTask;
    }

    public class WithoutAttributeMigration : Migration
    {
        public WithoutAttributeMigration() :
            base("1.2.4")
        {
        }

        public override Task UpdateAsync() => Task.CompletedTask;
    }

    [Parallelizable(ParallelScope.All)]
    public class ExcludeExperimentalMigrationsTest
    {
        [Test]
        public void HasExperimentalAttributeNull()
        {
            ExcludeExperimentalMigrations.HasExperimentalAttribute(null)
                .Should().BeFalse();
        }

        [Test]
        public void HasExperimentalAttributeTrue()
        {
            ExcludeExperimentalMigrations.HasExperimentalAttribute(new WithAttributeMigration())
                .Should().BeTrue();
        }

        [Test]
        public void HasExperimentalAttributeFalse()
        {
            ExcludeExperimentalMigrations.HasExperimentalAttribute(new WithoutAttributeMigration())
                .Should().BeFalse();
        }

        [Test]
        public void FilterWorks()
        {
            var filter = new ExcludeExperimentalMigrations();
            filter.Exclude(new WithAttributeMigration()).Should().BeTrue();
            filter.Exclude(new WithoutAttributeMigration()).Should().BeFalse();
        }
    }
}
