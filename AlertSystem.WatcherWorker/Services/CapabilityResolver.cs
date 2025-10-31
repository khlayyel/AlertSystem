using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace AlertSystem.WatcherWorker.Services;

public interface ICapabilityResolver
{
    IReadOnlyList<string> GetActionsForDomain(string domainCode);
}

public sealed class CapabilityResolver : ICapabilityResolver
{
    private readonly IConfiguration _configuration;

    public CapabilityResolver(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IReadOnlyList<string> GetActionsForDomain(string domainCode)
    {
        // appsettings:  "WatcherCapabilities": { "RESA": ["RESA_MANAGE"], ... }
        var section = _configuration.GetSection("WatcherCapabilities:" + domainCode);
        var values = section.Get<string[]>() ?? domainCode switch
        {
            "RESA" => new[] { "RESA_MANAGE" },
            "TPV" => new[] { "TPV_MONITOR" },
            "COMPTA" => new[] { "AR_OVERDUE" },
            "GRH" => new[] { "HR_ABS_APPROVE", "HR_CONTRACTS" },
            "BNQ" => new[] { "BNQ_EVENT" },
            "TRES" => new[] { "TREASURY_ALERT" },
            _ => new string[0]
        };
        return values;
    }
}


