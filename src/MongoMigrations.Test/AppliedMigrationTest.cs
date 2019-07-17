using FluentAssertions;
using NUnit.Framework;

namespace MongoMigrations.Test
{
    [Parallelizable(ParallelScope.All)]
    public class AppliedMigrationTest
    {
        [Test]
        public void DefaultCtor()
        {
            new AppliedMigration();
        }

        [Test]
        public void CheckToString()
        {
            var am = new AppliedMigration(new TestCollectionMigration());
            am.ToString().Should().StartWith("1.0.1 started on");
        }

        [Test]
        public void MarkerOnly()
        {
            var am = AppliedMigration.MarkerOnly("2.0.0");
            am.ToString().Should().StartWith("2.0.0 started on");
        }
    }
}
