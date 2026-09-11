using UnifiedAI.Api.Models;

namespace UnifiedAI.Api.Connectors;

public sealed class ClaudeConnector : IProviderConnector
{
    public string ProviderType=>"Claude";
    public Task<string> GetRawUsageAsync(IntegrationConfiguration configuration,CancellationToken cancellationToken=default)=>Task.FromResult("""
    {
      "organization": "enterprise-claude",
      "usage": {
        "requests": 430250,
        "input_tokens": 8450000,
        "output_tokens": 3250000,
        "successful_requests": 427670,
        "failed_requests": 2580
      }
    }
    """);
}

public sealed class OpenAIConnector : IProviderConnector
{
    public string ProviderType=>"OpenAI";
    public Task<string> GetRawUsageAsync(IntegrationConfiguration configuration,CancellationToken cancellationToken=default)=>Task.FromResult("""
    {
      "account": "openai-production",
      "metrics": {
        "api_calls": 385800,
        "prompt_token_count": 6750000,
        "completion_token_count": 2650000,
        "success": 382328,
        "errors": 3472,
        "amount": 1340.25
      }
    }
    """);
}

public sealed class CopilotConnector : IProviderConnector
{
    public string ProviderType=>"Copilot";
    public Task<string> GetRawUsageAsync(IntegrationConfiguration configuration,CancellationToken cancellationToken=default)=>Task.FromResult("""
    {
      "tenantName": "m365-enterprise",
      "consumption": {
        "calls": 248600,
        "tokens": {
          "in": 4150000,
          "out": 1860000
        },
        "outcomes": {
          "ok": 246975,
          "failed": 1625
        }
      },
      "modelFamily": "microsoft-copilot"
    }
    """);
}

public sealed class ServiceNowConnector : IProviderConnector
{
    public string ProviderType=>"ServiceNow";
    public Task<string> GetRawUsageAsync(IntegrationConfiguration configuration,CancellationToken cancellationToken=default)=>Task.FromResult("""
    {
      "instance": "servicenow-prod",
      "ai_usage": {
        "request_total": 142900,
        "input": 2190000,
        "output": 960000,
        "completed": 138613,
        "errored": 4287,
        "spend_usd": 410.75
      },
      "timestamp_utc": "2026-09-10T08:30:00Z"
    }
    """);
}
