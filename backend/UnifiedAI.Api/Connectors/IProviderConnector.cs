using UnifiedAI.Api.Models;

namespace UnifiedAI.Api.Connectors;

public interface IProviderConnector
{
    string ProviderType { get; }
    Task<string> GetRawUsageAsync(IntegrationConfiguration configuration,CancellationToken cancellationToken=default);
}
