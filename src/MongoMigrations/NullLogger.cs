namespace MongoMigrations
{
    using System;
    using Microsoft.Extensions.Logging;

    internal sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new NullScope();

        private NullScope()
        {
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }

    public class NullLogger<T> : ILogger<T>
    {
        public static NullLogger<T> Instance { get; } = new NullLogger<T>();

        private NullLogger()
        {
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return false;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
        }
    }
}
