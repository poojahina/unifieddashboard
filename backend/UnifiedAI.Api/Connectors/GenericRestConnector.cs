using UnifiedAI.Api.Models;

namespace UnifiedAI.Api.Connectors;

public sealed class GenericRestConnector(IHttpClientFactory httpClientFactory) : IProviderConnector
{
    public string ProviderType=>"Custom";

    public async Task<string> GetRawUsageAsync(IntegrationConfiguration configuration,CancellationToken cancellationToken=default)
    {
        if(string.IsNullOrWhiteSpace(configuration.BaseUrl)||string.IsNullOrWhiteSpace(configuration.UsageEndpoint))
        {
            return $$"""
            {
              "providerName": "{{configuration.ProviderType}}",
              "account": "{{configuration.IntegrationName}}",
              "usageStats": {
                "totalRequests": 100,
                "promptTokens": 5000,
                "completionTokens": 2000,
                "successful": 96,
                "failed": 4
              }
            }
            """;
        }

        var client=httpClientFactory.CreateClient();
        client.BaseAddress=new Uri(configuration.BaseUrl,UriKind.Absolute);
        if(!string.IsNullOrWhiteSpace(configuration.ApiKey))
        {
            if(string.Equals(configuration.AuthenticationType,"Bearer Token",StringComparison.OrdinalIgnoreCase))
                client.DefaultRequestHeaders.Authorization=new("Bearer",configuration.ApiKey);
            else if(string.Equals(configuration.AuthenticationType,"Custom Header",StringComparison.OrdinalIgnoreCase)&&!string.IsNullOrWhiteSpace(configuration.CustomHeaderName))
                client.DefaultRequestHeaders.TryAddWithoutValidation(configuration.CustomHeaderName,configuration.ApiKey);
            else
                client.DefaultRequestHeaders.TryAddWithoutValidation("x-api-key",configuration.ApiKey);
        }

        var response=await client.GetAsync(configuration.UsageEndpoint,cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}

public sealed class ConnectorResolver(IEnumerable<IProviderConnector> connectors,GenericRestConnector genericConnector)
{
    public IProviderConnector Resolve(string providerType)
    {
        var connector=connectors.FirstOrDefault(x=>x.ProviderType.Equals(providerType,StringComparison.OrdinalIgnoreCase));
        return connector??genericConnector;
    }
}
