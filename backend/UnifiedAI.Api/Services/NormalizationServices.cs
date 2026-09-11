using System.Text.Json;
using System.Text.Json.Nodes;
using UnifiedAI.Api.Connectors;
using UnifiedAI.Api.Models;

namespace UnifiedAI.Api.Services;

public interface ILlmClient
{
    Task<string> NormalizeAsync(string systemPrompt,string provider,string rawJson,CancellationToken cancellationToken=default);
}

public interface IUsageNormalizer
{
    Task<List<UnifiedUsageMetric>> NormalizeAsync(string provider,string rawJson,CancellationToken cancellationToken=default);
}

public sealed class JsonSanitizer
{
    private static readonly string[] SecretNames=["apikey","api_key","authorization","accesstoken","access_token","refreshtoken","refresh_token","clientsecret","client_secret","password","secret"];

    public string Sanitize(string rawJson)
    {
        var node=JsonNode.Parse(rawJson);
        if(node==null) return rawJson;
        Scrub(node);
        return node.ToJsonString(new JsonSerializerOptions{WriteIndented=true});
    }

    private static void Scrub(JsonNode node)
    {
        if(node is JsonObject obj)
        {
            foreach(var key in obj.Select(x=>x.Key).ToArray())
            {
                if(SecretNames.Any(secret=>key.Replace("-","",StringComparison.OrdinalIgnoreCase).Equals(secret,StringComparison.OrdinalIgnoreCase)||key.Contains(secret,StringComparison.OrdinalIgnoreCase)))
                    obj.Remove(key);
                else if(obj[key] is { } child)
                    Scrub(child);
            }
        }
        else if(node is JsonArray array)
        {
            foreach(var child in array.OfType<JsonNode>()) Scrub(child);
        }
    }
}

public sealed class DemoLlmClient : ILlmClient
{
    private static readonly JsonSerializerOptions JsonOptions=new(){PropertyNamingPolicy=JsonNamingPolicy.CamelCase,WriteIndented=true};

    public Task<string> NormalizeAsync(string systemPrompt,string provider,string rawJson,CancellationToken cancellationToken=default)
    {
        var root=JsonNode.Parse(rawJson)??new JsonObject();
        var metric=new UnifiedUsageMetric
        {
            Provider=provider,
            AccountName=FirstString(root,"organization","account","tenantName","instance","providerName"),
            Timestamp=FirstDate(root,"timestamp","timestamp_utc"),
            RequestCount=FirstLong(root,"requests","api_calls","calls","request_total","totalRequests"),
            InputTokens=FirstLong(root,"input_tokens","prompt_token_count","in","input","promptTokens"),
            OutputTokens=FirstLong(root,"output_tokens","completion_token_count","out","output","completionTokens"),
            Cost=FirstDecimal(root,"amount","spend_usd","cost"),
            Currency=FirstDecimal(root,"spend_usd")!=null||FirstDecimal(root,"amount")!=null?"USD":null,
            SuccessfulRequests=FirstLong(root,"successful_requests","success","ok","completed","successful"),
            FailedRequests=FirstLong(root,"failed_requests","errors","failed","errored"),
            Model=FirstString(root,"model","modelFamily")
        };
        return Task.FromResult(JsonSerializer.Serialize(new[]{metric},JsonOptions));
    }

    private static string? FirstString(JsonNode node,params string[] names)=>Descendants(node).OfType<JsonObject>().SelectMany(x=>x).Where(x=>names.Contains(x.Key,StringComparer.OrdinalIgnoreCase)).Select(x=>x.Value?.GetValue<string>()).FirstOrDefault(x=>!string.IsNullOrWhiteSpace(x));
    private static long? FirstLong(JsonNode node,params string[] names)=>Descendants(node).OfType<JsonObject>().SelectMany(x=>x).Where(x=>names.Contains(x.Key,StringComparer.OrdinalIgnoreCase)).Select(x=>AsLong(x.Value)).FirstOrDefault(x=>x.HasValue);
    private static decimal? FirstDecimal(JsonNode node,params string[] names)=>Descendants(node).OfType<JsonObject>().SelectMany(x=>x).Where(x=>names.Contains(x.Key,StringComparer.OrdinalIgnoreCase)).Select(x=>AsDecimal(x.Value)).FirstOrDefault(x=>x.HasValue);
    private static DateTime? FirstDate(JsonNode node,params string[] names)=>Descendants(node).OfType<JsonObject>().SelectMany(x=>x).Where(x=>names.Contains(x.Key,StringComparer.OrdinalIgnoreCase)).Select(x=>DateTime.TryParse(x.Value?.ToString(),out var date)?date:(DateTime?)null).FirstOrDefault(x=>x.HasValue);
    private static IEnumerable<JsonNode> Descendants(JsonNode node)
    {
        yield return node;
        if(node is JsonObject obj) foreach(var child in obj.Select(x=>x.Value).OfType<JsonNode>()) foreach(var descendant in Descendants(child)) yield return descendant;
        if(node is JsonArray array) foreach(var child in array.OfType<JsonNode>()) foreach(var descendant in Descendants(child)) yield return descendant;
    }
    private static long? AsLong(JsonNode? node)=>long.TryParse(node?.ToString(),out var value)?value:null;
    private static decimal? AsDecimal(JsonNode? node)=>decimal.TryParse(node?.ToString(),out var value)?value:null;
}

public sealed class LlmUsageNormalizer(ILlmClient llmClient,ILogger<LlmUsageNormalizer> logger) : IUsageNormalizer
{
    private const string SystemPrompt="""
    You are an AI usage data normalization engine.
    Convert provider-specific AI usage JSON into the supplied canonical schema.
    Never invent values. If unavailable, return null. Return JSON only.
    Map input/prompt tokens to inputTokens, output/completion tokens to outputTokens,
    API calls/requests to requestCount, successful calls to successfulRequests,
    and errors/failed calls to failedRequests.
    """;
    private static readonly JsonSerializerOptions Options=new(){PropertyNameCaseInsensitive=true};

    public async Task<List<UnifiedUsageMetric>> NormalizeAsync(string provider,string rawJson,CancellationToken cancellationToken=default)
    {
        try
        {
            var json=await llmClient.NormalizeAsync(SystemPrompt,provider,rawJson,cancellationToken);
            return JsonSerializer.Deserialize<List<UnifiedUsageMetric>>(json,Options)??[];
        }
        catch(Exception ex)
        {
            logger.LogWarning("Normalization failed for {Provider}; response size {ResponseSize}; error type {ErrorType}",provider,rawJson.Length,ex.GetType().Name);
            return [new UnifiedUsageMetric{Provider=provider,Status="NormalizationFailed",ErrorMessage="Provider response could not be normalized."}];
        }
    }
}

public sealed class UsageValidator
{
    public List<UnifiedUsageMetric> ValidateAndCalculate(IEnumerable<UnifiedUsageMetric> metrics)
    {
        var rows=metrics.ToList();
        foreach(var row in rows)
        {
            EnsureNonNegative(row.RequestCount,"requestCount");
            EnsureNonNegative(row.InputTokens,"inputTokens");
            EnsureNonNegative(row.OutputTokens,"outputTokens");
            EnsureNonNegative(row.Cost,"cost");
            EnsureNonNegative(row.SuccessfulRequests,"successfulRequests");
            EnsureNonNegative(row.FailedRequests,"failedRequests");
            if(row.InputTokens.HasValue&&row.OutputTokens.HasValue) row.TotalTokens=row.InputTokens.Value+row.OutputTokens.Value;
            if(row.RequestCount.HasValue&&row.SuccessfulRequests.HasValue&&row.FailedRequests.HasValue&&row.SuccessfulRequests.Value+row.FailedRequests.Value>row.RequestCount.Value)
                throw new DemoValidationException("Success and failure counts cannot exceed request count.");
        }
        return rows;
    }

    private static void EnsureNonNegative(long? value,string name) { if(value<0) throw new DemoValidationException($"{name} cannot be negative."); }
    private static void EnsureNonNegative(decimal? value,string name) { if(value<0) throw new DemoValidationException($"{name} cannot be negative."); }
}

public sealed class IntegrationService(ConnectorResolver resolver,JsonSanitizer sanitizer,IUsageNormalizer normalizer,UsageValidator validator)
{
    public async Task<List<UnifiedUsageMetric>> GetUsageAsync(IntegrationConfiguration configuration,CancellationToken cancellationToken=default)
    {
        var connector=resolver.Resolve(configuration.ProviderType);
        var rawJson=await connector.GetRawUsageAsync(configuration,cancellationToken);
        var sanitized=sanitizer.Sanitize(rawJson);
        var normalized=await normalizer.NormalizeAsync(configuration.ProviderType,sanitized,cancellationToken);
        return validator.ValidateAndCalculate(normalized);
    }

    public Task<List<UnifiedUsageMetric>> NormalizeRawAsync(string provider,string rawJson,CancellationToken cancellationToken=default)
    {
        var sanitized=sanitizer.Sanitize(rawJson);
        return NormalizeSanitizedAsync(provider,sanitized,cancellationToken);
    }

    private async Task<List<UnifiedUsageMetric>> NormalizeSanitizedAsync(string provider,string sanitized,CancellationToken cancellationToken)
    {
        var normalized=await normalizer.NormalizeAsync(provider,sanitized,cancellationToken);
        return validator.ValidateAndCalculate(normalized);
    }
}
