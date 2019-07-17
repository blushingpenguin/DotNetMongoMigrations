using FluentAssertions;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System;

namespace MongoMigrations.Test
{
    public class NullLoggerTest
    {
        [Test]
        public void BeginScope()
        {
            object stateObj = new object();
            using (var scope = NullLogger<object>.Instance.BeginScope(stateObj))
            {
            }
        }

        [Test]
        public void IsNotEnabled()
        {
            NullLogger<int>.Instance.IsEnabled(LogLevel.Information)
                .Should().BeFalse();
        }

        [Test]
        public void Log()
        {
            object stateObj = new object();
            NullLogger<Exception>.Instance.Log<object>(
                LogLevel.Error,
                new EventId(5, "test"),
                stateObj,
                null,
                (state, ex) => ""
            );
        }
    }
}
