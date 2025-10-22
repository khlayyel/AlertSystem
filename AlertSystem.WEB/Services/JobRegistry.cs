using System.Collections.Concurrent;

namespace AlertSystem.WEB.Services
{
    public interface IJobRegistry
    {
        void Set(int alerteId, string jobId);
        bool TryGet(int alerteId, out string jobId);
        bool Remove(int alerteId);
    }

    public sealed class InMemoryJobRegistry : IJobRegistry
    {
        private readonly ConcurrentDictionary<int, string> _jobs = new();

        public void Set(int alerteId, string jobId)
        {
            _jobs[alerteId] = jobId;
        }

        public bool TryGet(int alerteId, out string jobId)
        {
            return _jobs.TryGetValue(alerteId, out jobId!);
        }

        public bool Remove(int alerteId)
        {
            return _jobs.TryRemove(alerteId, out _);
        }
    }
}


