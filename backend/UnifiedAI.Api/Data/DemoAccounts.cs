using UnifiedAI.Api.Models;
namespace UnifiedAI.Api.Data;

public static class DemoAccounts
{
    public static readonly IReadOnlyList<AccountDefinition> All=[
        new("claude-prod","Enterprise-Claude-Prod","Claude","Production","Engineering","Claude Sonnet"),
        new("claude-dev","Claude-Development","Claude","Development","AI COE","Claude Opus"),
        new("openai-prod","OpenAI-Production","OpenAI","Production","Customer Support","GPT-4.1"),
        new("openai-dev","OpenAI-Development","OpenAI","Test","Sales","GPT-4.1 mini"),
        new("copilot-corp","Corporate-Copilot","Copilot","Production","Engineering","Microsoft 365 Copilot"),
        new("servicenow-prod","ServiceNow-Production","ServiceNow","Production","Operations","Now Assist")
    ];
}

