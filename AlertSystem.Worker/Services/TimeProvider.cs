using System;

namespace AlertSystem.Worker.Services;

public interface ITimeProvider
{
    DateTime UtcNow { get; }
}

public sealed class SystemTimeProvider : ITimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}


