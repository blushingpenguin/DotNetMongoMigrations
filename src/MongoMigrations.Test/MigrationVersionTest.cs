using FluentAssertions;
using NUnit.Framework;
using System;

namespace MongoMigrations.Test
{
    [Parallelizable(ParallelScope.All)]
    public class MigrationVersionTest
    {
        [Test]
        public void Default()
        {
            MigrationVersion.Default().ToString().Should().Be("0.0.0");
        }

        [Test]
        public void ConstructTooManyDigits()
        {
            Action a = () => new MigrationVersion("1.2.3.4");
            a.Should().Throw<ArgumentException>().And.
                Message.Should().StartWith("Versions must have format: major.minor.revision");
        }

        [Test]
        public void ConstructBadMajor()
        {
            Action a = () => new MigrationVersion("X.2.3");
            a.Should().Throw<ArgumentException>().And.
                Message.Should().StartWith("Invalid major version value: X");
        }

        [Test]
        public void ConstructBadMinor()
        {
            Action a = () => new MigrationVersion("1.X.3");
            a.Should().Throw<ArgumentException>().And.
                Message.Should().StartWith("Invalid minor version value: X");
        }

        [Test]
        public void ConstructBadRevision()
        {
            Action a = () => new MigrationVersion("1.2.X");
            a.Should().Throw<ArgumentException>().And.
                Message.Should().StartWith("Invalid revision version value: X");
        }

        [Test]
        public void ConstructExplicitComponents()
        {
            var mv = new MigrationVersion(1, 2, 3);
            mv.Major.Should().Be(1);
            mv.Minor.Should().Be(2);
            mv.Revision.Should().Be(3);
        }

        [Test]
        public void EqualsChecksMajor()
        {
            var mv1 = new MigrationVersion(1, 2, 3);
            var mv2 = new MigrationVersion(2, 2, 3);
            mv1.Equals(mv2).Should().BeFalse();
        }

        [Test]
        public void EqualsChecksMinor()
        {
            var mv1 = new MigrationVersion(1, 2, 3);
            var mv2 = new MigrationVersion(1, 3, 3);
            mv1.Equals(mv2).Should().BeFalse();
        }

        [Test]
        public void EqualsChecksRevision()
        {
            var mv1 = new MigrationVersion(1, 2, 3);
            var mv2 = new MigrationVersion(1, 2, 4);
            mv1.Equals(mv2).Should().BeFalse();
        }

        [Test]
        public void EqualsChecksAll()
        {
            var mv1 = new MigrationVersion(1, 2, 3);
            var mv2 = new MigrationVersion(1, 2, 3);
            (mv1 == mv2).Should().BeTrue();
        }

        [Test]
        public void NotEquals()
        {
            var mv1 = new MigrationVersion(1, 2, 3);
            var mv2 = new MigrationVersion(1, 2, 4);
            (mv1 != mv2).Should().BeTrue();
        }

        [Test]
        public void LessThanChecksMajor()
        {
            var mv1 = new MigrationVersion(1, 2, 3);
            var mv2 = new MigrationVersion(2, 2, 3);
            (mv1 < mv2).Should().BeTrue();
        }

        [Test]
        public void LessThanChecksMajorInverted()
        {
            var mv1 = new MigrationVersion(1, 2, 3);
            var mv2 = new MigrationVersion(0, 2, 3);
            (mv1 < mv2).Should().BeFalse();
        }

        [Test]
        public void LessThanChecksMinor()
        {
            var mv1 = new MigrationVersion(1, 2, 3);
            var mv2 = new MigrationVersion(1, 3, 3);
            (mv1 < mv2).Should().BeTrue();
        }

        [Test]
        public void LessThanChecksMinorInverted()
        {
            var mv1 = new MigrationVersion(1, 3, 3);
            var mv2 = new MigrationVersion(1, 2, 3);
            (mv1 < mv2).Should().BeFalse();
        }

        [Test]
        public void LessThanChecksRevision()
        {
            var mv1 = new MigrationVersion(1, 2, 3);
            var mv2 = new MigrationVersion(1, 2, 4);
            (mv1 < mv2).Should().BeTrue();
        }

        [Test]
        public void LessThanChecksRevisionInverted()
        {
            var mv1 = new MigrationVersion(1, 2, 4);
            var mv2 = new MigrationVersion(1, 2, 3);
            (mv1 < mv2).Should().BeFalse();
        }

        [Test]
        public void LessThanNotEqual()
        {
            var mv1 = new MigrationVersion(1, 2, 3);
            var mv2 = new MigrationVersion(1, 2, 3);
            (mv1 < mv2).Should().BeFalse();
        }

        [Test]
        public void GreaterThanNotEqual()
        {
            var mv1 = new MigrationVersion(1, 2, 3);
            var mv2 = new MigrationVersion(1, 2, 3);
            (mv1 > mv2).Should().BeFalse();
        }

        [Test]
        public void GreaterThanTrue()
        {
            var mv1 = new MigrationVersion(2, 2, 3);
            var mv2 = new MigrationVersion(1, 2, 3);
            (mv1 > mv2).Should().BeTrue();
        }

        [Test]
        public void GreaterThanFalse()
        {
            var mv1 = new MigrationVersion(1, 2, 3);
            var mv2 = new MigrationVersion(2, 2, 3);
            (mv1 > mv2).Should().BeFalse();
        }

        [Test]
        public void GreaterThanOrEqualToEqual()
        {
            var mv1 = new MigrationVersion(1, 2, 3);
            var mv2 = new MigrationVersion(1, 2, 3);
            (mv1 >= mv2).Should().BeTrue();
        }

        [Test]
        public void GreaterThanOrEqualToTrue()
        {
            var mv1 = new MigrationVersion(2, 2, 3);
            var mv2 = new MigrationVersion(1, 2, 3);
            (mv1 >= mv2).Should().BeTrue();
        }

        [Test]
        public void GreaterThanOrEqualToFalse()
        {
            var mv1 = new MigrationVersion(1, 2, 3);
            var mv2 = new MigrationVersion(2, 2, 3);
            (mv1 >= mv2).Should().BeFalse();
        }

        [Test]
        public void LessThanOrEqualToEqual()
        {
            var mv1 = new MigrationVersion(1, 2, 3);
            var mv2 = new MigrationVersion(1, 2, 3);
            (mv1 <= mv2).Should().BeTrue();
        }

        [Test]
        public void LessThanOrEqualToTrue()
        {
            var mv1 = new MigrationVersion(1, 2, 3);
            var mv2 = new MigrationVersion(2, 2, 3);
            (mv1 <= mv2).Should().BeTrue();
        }

        [Test]
        public void LessThanOrEqualToFalse()
        {
            var mv1 = new MigrationVersion(2, 2, 3);
            var mv2 = new MigrationVersion(1, 2, 3);
            (mv1 <= mv2).Should().BeFalse();
        }

        [Test]
        public void CompareToEqual()
        {
            var mv1 = new MigrationVersion(1, 2, 3);
            var mv2 = new MigrationVersion(1, 2, 3);
            mv1.CompareTo(mv2).Should().Be(0);
        }

        [Test]
        public void CompareToGreater()
        {
            var mv1 = new MigrationVersion(2, 2, 3);
            var mv2 = new MigrationVersion(1, 2, 3);
            mv1.CompareTo(mv2).Should().Be(1);
        }

        [Test]
        public void CompareToLess()
        {
            var mv1 = new MigrationVersion(1, 2, 3);
            var mv2 = new MigrationVersion(2, 2, 3);
            mv1.CompareTo(mv2).Should().Be(-1);
        }

        [Test]
        public void ReferenceEqual()
        {
            var mv = new MigrationVersion(1, 2, 3);
            mv.Equals(mv).Should().BeTrue();
        }

        [Test]
        public void EqualsOtherObjectFalse()
        {
            var mv = new MigrationVersion(1, 2, 3);
            var o = new object();
            mv.Equals(o).Should().BeFalse();
        }

        [Test]
        public void EqualsNullObjectFalse()
        {
            var mv = new MigrationVersion(1, 2, 3);
            object o = null;
            mv.Equals(o).Should().BeFalse();
        }

        [Test]
        public void GetHashCodeReturnsDifferentCodesForDifferentPoints()
        {
            var mv1 = new MigrationVersion(1, 2, 3);
            var mv2 = new MigrationVersion(1, 2, 4);
            int h1 = mv1.GetHashCode();
            h1.Should().NotBe(0);
            var h2 = mv2.GetHashCode();
            h2.Should().NotBe(0);
            h1.Should().NotBe(h2);
        }

        [Test]
        public void GetHashCodeReturnsSameCodesForSamePoints()
        {
            var mv1 = new MigrationVersion(1, 2, 3);
            var mv2 = new MigrationVersion(1, 2, 3);
            int h1 = mv1.GetHashCode();
            h1.Should().NotBe(0);
            var h2 = mv2.GetHashCode();
            h2.Should().NotBe(0);
            h1.Should().Be(h2);
        }

        [Test]
        public void ToStringWorks()
        {
            var mv = new MigrationVersion(8, 5, 2);
            mv.ToString().Should().Be("8.5.2");
        }

        private MigrationVersion ImplicitTest(MigrationVersion mv)
        {
            return mv;
        }

        [Test]
        public void ImplicitConstructorWorks()
        {
            ImplicitTest("1.2.3").Should().BeEquivalentTo(new MigrationVersion(1, 2, 3));
        }

        private string StringTest(MigrationVersion mv)
        {
            return mv;
        }

        [Test]
        public void ImplicitStringOperatorWorks()
        {
            var mv = new MigrationVersion(4, 5, 6);
            StringTest(mv).Should().Be("4.5.6");
        }
    }
}
