using FluentAssertions;
using NUnit.Framework;
using System;

namespace MongoMigrations.Test
{
    [Parallelizable(ParallelScope.All)]
    public class MigrationVersionTest
    {
        [Test]
        public void ConstructNull()
        {
            Action a = () => new MigrationVersion(null);
            a.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("version");
        }

        [Test]
        public void ConstructEmpty()
        {
            Action a = () => new MigrationVersion("");
            a.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("version");
        }

        [Test]
        public void Default()
        {
            MigrationVersion.Default.ToString().Should().Be("M00010101000000_");
        }

        [Test]
        public void ConstructMissingUnderscore()
        {
            Action a = () => new MigrationVersion("M20010203040506test");
            a.Should().Throw<ArgumentException>().And.
                Message.Should().StartWith("The migration version (M20010203040506test) must be of the format MyyyyMMddHHmmss_Name");
        }

        [Test]
        public void ConstructBadLength()
        {
            Action a = () => new MigrationVersion("1.2.3");
            a.Should().Throw<ArgumentException>().And.
                Message.Should().StartWith("The migration version (1.2.3) must be of the format MyyyyMMddHHmmss_Name");
        }

        [Test]
        public void ConstructBadPrefix()
        {
            Action a = () => new MigrationVersion("X20010101000000_test");
            a.Should().Throw<ArgumentException>().And.
                Message.Should().StartWith("The migration version (X20010101000000_test) must start with the letter M");
        }

        [Test]
        public void ConstructBadYear()
        {
            Action a = () => new MigrationVersion("M00000000000000_name");
            a.Should().Throw<ArgumentException>().And.
                Message.Should().StartWith("The year component (0000) of the version (M00000000000000_name) is invalid");
        }

        [Test]
        public void ConstructBadMonth()
        {
            Action a = () => new MigrationVersion("M20011400000000_name");
            a.Should().Throw<ArgumentException>().And.
                Message.Should().StartWith("The month component (14) of the version (M20011400000000_name) is invalid");
        }

        [Test]
        public void ConstructBadDay()
        {
            Action a = () => new MigrationVersion("M20011142000000_name");
            a.Should().Throw<ArgumentException>().And.
                Message.Should().StartWith("The day component (42) of the version (M20011142000000_name) is invalid");
        }

        [Test]
        public void ConstructBadHour()
        {
            Action a = () => new MigrationVersion("M20011124990000_name");
            a.Should().Throw<ArgumentException>().And.
                Message.Should().StartWith("The hour component (99) of the version (M20011124990000_name) is invalid");
        }

        [Test]
        public void ConstructBadMinute()
        {
            Action a = () => new MigrationVersion("M20011123117600_name");
            a.Should().Throw<ArgumentException>().And.
                Message.Should().StartWith("The minute component (76) of the version (M20011123117600_name) is invalid");
        }

        [Test]
        public void ConstructBadSecond()
        {
            Action a = () => new MigrationVersion("M20011123235988_name");
            a.Should().Throw<ArgumentException>().And.
                Message.Should().StartWith("The second component (88) of the version (M20011123235988_name) is invalid");
        }

        [Test]
        public void ConstructGood()
        {
            var version = new MigrationVersion("M20010304050607_alpha");
            version.Timestamp.Should().Be(new DateTime(2001, 3, 4, 5, 6, 7));
            version.Name.Should().Be("alpha");
        }

        [Test]
        public void ConstructExplicitComponents()
        {
            var dt = new DateTime(2012, 9, 24, 11, 46, 12);
            var mv = new MigrationVersion(dt, "test");
            mv.Timestamp.Should().Be(dt);
            mv.Name.Should().Be("test");
        }

        [Test]
        public void EqualsChecksTimestamp()
        {
            var mv1 = new MigrationVersion(new DateTime(2001, 1, 1), "test");
            var mv2 = new MigrationVersion(new DateTime(2002, 1, 1), "test");
            mv1.Equals(mv2).Should().BeFalse();
        }

        [Test]
        public void EqualsChecksName()
        {
            var mv1 = new MigrationVersion(new DateTime(2001, 1, 1), "alpha");
            var mv2 = new MigrationVersion(new DateTime(2001, 1, 1), "beta");
            mv1.Equals(mv2).Should().BeFalse();
        }

        [Test]
        public void EqualsChecksAll()
        {
            var mv1 = new MigrationVersion(new DateTime(2001, 1, 1), "alpha");
            var mv2 = new MigrationVersion(new DateTime(2001, 1, 1), "alpha");
            (mv1 == mv2).Should().BeTrue();
        }

        [Test]
        public void NotEquals()
        {
            var mv1 = new MigrationVersion(new DateTime(2001, 1, 1), "alpha");
            var mv2 = new MigrationVersion(new DateTime(2001, 1, 1), "beta");
            (mv1 != mv2).Should().BeTrue();
        }

        [Test]
        public void LessThanChecksTimestamp()
        {
            var mv1 = new MigrationVersion(new DateTime(2001, 1, 1), "test");
            var mv2 = new MigrationVersion(new DateTime(2002, 1, 1), "test");
            (mv1 < mv2).Should().BeTrue();
        }

        [Test]
        public void LessThanChecksTimestampInverted()
        {
            var mv1 = new MigrationVersion(new DateTime(2001, 1, 1), "test");
            var mv2 = new MigrationVersion(new DateTime(2000, 1, 1), "test");
            (mv1 < mv2).Should().BeFalse();
        }

        [Test]
        public void LessThanChecksName()
        {
            var mv1 = new MigrationVersion(new DateTime(2001, 1, 1), "alpha");
            var mv2 = new MigrationVersion(new DateTime(2001, 1, 1), "beta");
            (mv1 < mv2).Should().BeTrue();
        }

        [Test]
        public void LessThanChecksNameInverted()
        {
            var mv1 = new MigrationVersion(new DateTime(2001, 1, 1), "beta");
            var mv2 = new MigrationVersion(new DateTime(2001, 1, 1), "alpha");
            (mv1 < mv2).Should().BeFalse();
        }

        [Test]
        public void LessThanNotEqual()
        {
            var mv1 = new MigrationVersion(new DateTime(2001, 1, 1), "test");
            var mv2 = new MigrationVersion(new DateTime(2001, 1, 1), "test");
            (mv1 < mv2).Should().BeFalse();
        }

        [Test]
        public void GreaterThanNotEqual()
        {
            var mv1 = new MigrationVersion(new DateTime(2001, 1, 1), "test");
            var mv2 = new MigrationVersion(new DateTime(2001, 1, 1), "test");
            (mv1 > mv2).Should().BeFalse();
        }

        [Test]
        public void GreaterThanTrue()
        {
            var mv1 = new MigrationVersion(new DateTime(2002, 1, 1), "test");
            var mv2 = new MigrationVersion(new DateTime(2001, 1, 1), "test");
            (mv1 > mv2).Should().BeTrue();
        }

        [Test]
        public void GreaterThanFalse()
        {
            var mv1 = new MigrationVersion(new DateTime(2001, 1, 1), "test");
            var mv2 = new MigrationVersion(new DateTime(2002, 1, 1), "test");
            (mv1 > mv2).Should().BeFalse();
        }

        [Test]
        public void GreaterThanOrEqualToEqual()
        {
            var mv1 = new MigrationVersion(new DateTime(2001, 1, 1), "test");
            var mv2 = new MigrationVersion(new DateTime(2001, 1, 1), "test");
            (mv1 >= mv2).Should().BeTrue();
        }

        [Test]
        public void GreaterThanOrEqualToTrue()
        {
            var mv1 = new MigrationVersion(new DateTime(2002, 1, 1), "test");
            var mv2 = new MigrationVersion(new DateTime(2001, 1, 1), "test");
            (mv1 >= mv2).Should().BeTrue();
        }

        [Test]
        public void GreaterThanOrEqualToFalse()
        {
            var mv1 = new MigrationVersion(new DateTime(2001, 1, 1), "test");
            var mv2 = new MigrationVersion(new DateTime(2002, 1, 1), "test");
            (mv1 >= mv2).Should().BeFalse();
        }

        [Test]
        public void LessThanOrEqualToEqual()
        {
            var mv1 = new MigrationVersion(new DateTime(2001, 1, 1), "test");
            var mv2 = new MigrationVersion(new DateTime(2001, 1, 1), "test");
            (mv1 <= mv2).Should().BeTrue();
        }

        [Test]
        public void LessThanOrEqualToTrue()
        {
            var mv1 = new MigrationVersion(new DateTime(2001, 1, 1), "test");
            var mv2 = new MigrationVersion(new DateTime(2002, 1, 1), "test");
            (mv1 <= mv2).Should().BeTrue();
        }

        [Test]
        public void LessThanOrEqualToFalse()
        {
            var mv1 = new MigrationVersion(new DateTime(2002, 1, 1), "test");
            var mv2 = new MigrationVersion(new DateTime(2001, 1, 1), "test");
            (mv1 <= mv2).Should().BeFalse();
        }

        [Test]
        public void CompareToEqual()
        {
            var mv1 = new MigrationVersion(new DateTime(2001, 1, 1), "test");
            var mv2 = new MigrationVersion(new DateTime(2001, 1, 1), "test");
            mv1.CompareTo(mv2).Should().Be(0);
        }

        [Test]
        public void CompareToGreater()
        {
            var mv1 = new MigrationVersion(new DateTime(2002, 1, 1), "test");
            var mv2 = new MigrationVersion(new DateTime(2001, 1, 1), "test");
            mv1.CompareTo(mv2).Should().Be(1);
        }

        [Test]
        public void CompareToLess()
        {
            var mv1 = new MigrationVersion(new DateTime(2001, 1, 1), "test");
            var mv2 = new MigrationVersion(new DateTime(2002, 1, 1), "test");
            mv1.CompareTo(mv2).Should().Be(-1);
        }

        [Test]
        public void ReferenceEqual()
        {
            var mv = new MigrationVersion(new DateTime(2001, 1, 1), "test");
            mv.Equals(mv).Should().BeTrue();
        }

        [Test]
        public void EqualsOtherObjectFalse()
        {
            var mv = new MigrationVersion(new DateTime(2001, 1, 1), "test");
            var o = new object();
            mv.Equals(o).Should().BeFalse();
        }

        [Test]
        public void EqualsNullObjectFalse()
        {
            var mv = new MigrationVersion(new DateTime(2001, 1, 1), "test");
            object o = null;
            mv.Equals(o).Should().BeFalse();
        }

        [Test]
        public void GetHashCodeReturnsDifferentCodesForDifferentPoints()
        {
            var mv1 = new MigrationVersion(new DateTime(2001, 1, 1), "test");
            var mv2 = new MigrationVersion(new DateTime(2004, 1, 1), "test");
            int h1 = mv1.GetHashCode();
            h1.Should().NotBe(0);
            var h2 = mv2.GetHashCode();
            h2.Should().NotBe(0);
            h1.Should().NotBe(h2);
        }

        [Test]
        public void GetHashCodeReturnsSameCodesForSamePoints()
        {
            var mv1 = new MigrationVersion(new DateTime(2001, 1, 1), "test");
            var mv2 = new MigrationVersion(new DateTime(2001, 1, 1), "test");
            int h1 = mv1.GetHashCode();
            h1.Should().NotBe(0);
            var h2 = mv2.GetHashCode();
            h2.Should().NotBe(0);
            h1.Should().Be(h2);
        }

        [Test]
        public void ToStringWorks()
        {
            var mv = new MigrationVersion(new DateTime(2001, 1, 1), "test");
            mv.ToString().Should().Be("M20010101000000_test");
        }
    }
}
