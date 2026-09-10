using UnifiedAI.Api.Models;
namespace UnifiedAI.Api.Connectors;

// Future extension point. No real outbound provider calls are implemented in this POC.
public interface IAIProviderConnector
{
    string ProviderType {get;}
    Task<ConnectionTestResult> TestConnectionAsync(IReadOnlyDictionary<string,string> configuration,CancellationToken cancellationToken);
    Task<IReadOnlyList<UsageRecord>> GetUsageAsync(IReadOnlyDictionary<string,string> configuration,DateOnly from,DateOnly to,CancellationToken cancellationToken);
}
public interface IAIProviderConnectorResolver { IAIProviderConnector Resolve(string providerType); }

