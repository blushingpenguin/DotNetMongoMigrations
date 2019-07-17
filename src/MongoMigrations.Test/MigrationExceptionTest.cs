using FluentAssertions;
using NUnit.Framework;
using System;

namespace MongoMigrations.Test
{
    [Parallelizable(ParallelScope.All)]
    public class MigrationExceptionTest
    {
        [Test]
        public void ConstructWithMessage()
        {
            var me = new MigrationException("hello", new Exception("inner"));
            me.Message.Should().Be("hello");
            me.InnerException.Message.Should().Be("inner");
        }
    }
}
