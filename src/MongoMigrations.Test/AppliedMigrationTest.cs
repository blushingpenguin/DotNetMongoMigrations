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
            var am = new AppliedMigration(new M20190718162300_TestCollectionMigration());
            am.ToString().Should().StartWith("M20190718162300_TestCollectionMigration started on");
        }

        [Test]
        public void MarkerOnly()
        {
            var am = AppliedMigration.MarkerOnly("M20190806065900_test");
            am.ToString().Should().StartWith("M20190806065900_test started on");
        }
    }
}
