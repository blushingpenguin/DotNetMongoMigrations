using FluentAssertions;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace MongoMigrations.Test
{
    [Experimental]
    public class M20190718160123_WithAttributeMigration : Migration
    {
        public override Task UpdateAsync() => Task.CompletedTask;
    }

    public class M20190718160124_WithoutAttributeMigration : Migration
    {

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
            ExcludeExperimentalMigrations.HasExperimentalAttribute(new M20190718160123_WithAttributeMigration())
                .Should().BeTrue();
        }

        [Test]
        public void HasExperimentalAttributeFalse()
        {
            ExcludeExperimentalMigrations.HasExperimentalAttribute(new M20190718160124_WithoutAttributeMigration())
                .Should().BeFalse();
        }

        [Test]
        public void FilterWorks()
        {
            var filter = new ExcludeExperimentalMigrations();
            filter.Exclude(new M20190718160123_WithAttributeMigration()).Should().BeTrue();
            filter.Exclude(new M20190718160124_WithoutAttributeMigration()).Should().BeFalse();
        }
    }
}
