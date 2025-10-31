using System;

namespace AlertSystem.WatcherWorker.Services;

public interface ITimeProvider
{
    DateTime UtcNow { get; }
}

public sealed class SystemTimeProvider : ITimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}


