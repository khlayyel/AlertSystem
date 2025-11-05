using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace AlertSystem.Worker.Watchers
{
    public interface IHotelDomainPoller
    {
        Task PollAsync(CancellationToken ct);
        int IntervalSeconds { get; }
        bool Enabled { get; }
    }

    public sealed class PollingOrchestratorWorker : BackgroundService
    {
        private readonly ILogger<PollingOrchestratorWorker> _logger;
        private readonly IEnumerable<IHotelDomainPoller> _pollers;

        public PollingOrchestratorWorker(ILogger<PollingOrchestratorWorker> logger, IEnumerable<IHotelDomainPoller> pollers)
        { _logger = logger; _pollers = pollers; }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("PollingOrchestratorWorker started");

            var timers = _pollers.Where(p => p.Enabled).Select(p => RunLoop(p, stoppingToken)).ToArray();
            await Task.WhenAll(timers);

            _logger.LogInformation("PollingOrchestratorWorker stopping");
        }

        private async Task RunLoop(IHotelDomainPoller poller, CancellationToken ct)
        {
            var delay = Math.Max(5, poller.IntervalSeconds);
            while (!ct.IsCancellationRequested)
            {
                try { await poller.PollAsync(ct); }
                catch (Exception ex) { _logger.LogError(ex, "Poller error {Poller}", poller.GetType().Name); }
                await Task.Delay(TimeSpan.FromSeconds(delay), ct);
            }
        }
    }
}


