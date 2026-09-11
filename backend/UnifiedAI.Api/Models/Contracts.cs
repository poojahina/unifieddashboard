namespace UnifiedAI.Api.Models;

public sealed record ConfigurationField(string Name,string Label,string Type="text",bool Required=false,bool Secret=false,string[]? Options=null,string? Hint=null,string? RequiredWhenField=null,string[]? RequiredWhenValues=null);
public sealed record ProviderDefinition(string ProviderType,string DisplayName,string Color,string Monogram,string Description,Dictionary<string,string> Capabilities,ConfigurationField[] Fields);
public sealed record AccountDefinition(string Id,string Name,string Provider,string Environment,string Team,string Model);
public sealed record UsageRecord(string Id,DateOnly Date,string Provider,string AccountId,string AccountName,string Environment,string Team,string Model,long RequestCount,long? InputTokens,long? OutputTokens,decimal? Cost,long FailedRequests)
{
    public long? TotalTokens=>InputTokens+OutputTokens;
    public long SuccessfulRequests=>RequestCount-FailedRequests;
    public double SuccessRate=>RequestCount==0?0:100d*SuccessfulRequests/RequestCount;
    public string Currency=>"USD";
    public string CostType=>"EstimatedCost";
}
public sealed class UsageFilter
{
    public DateOnly? From {get;set;}
    public DateOnly? To {get;set;}
    public string? Provider {get;set;}
    public string? Account {get;set;}
    public string? Environment {get;set;}
    public string? Team {get;set;}
    public string? Model {get;set;}
    public string Metric {get;set;}="requests";
    public string Granularity {get;set;}="daily";
    public int Page {get;set;}=1;
    public int PageSize {get;set;}=25;
    public string SortBy {get;set;}="date";
    public string SortDirection {get;set;}="desc";
}
public sealed record DashboardSummary(long TotalRequests,long? InputTokens,long? OutputTokens,long? TotalTokens,decimal? EstimatedCost,double SuccessRate,int ActiveProviders,int ConnectedAccounts,double? RequestChangePercentage,double? TokenChangePercentage,double? CostChangePercentage,double ErrorRate,double? InputChangePercentage,double? OutputChangePercentage,double? ProviderChangePercentage,double? AccountChangePercentage,double? SuccessChangePercentage);
public sealed record ProviderSummary(string Provider,string DisplayName,string Color,string Monogram,string Status,string Health,long Requests,long? InputTokens,long? OutputTokens,long? TotalTokens,decimal? EstimatedCost,double SuccessRate,long FailedRequests,int ConnectedAccounts,DateTimeOffset LastSync);
public sealed record TrendPoint(DateOnly Date,string Provider,decimal? Value);
public sealed record Consumer(string Name,long Requests,long? Tokens,decimal? Cost);
public sealed record TopConsumers(IReadOnlyList<Consumer> Accounts,IReadOnlyList<Consumer> Models,IReadOnlyList<Consumer> Teams,IReadOnlyList<Consumer> Environments);
public sealed record PageResult<T>(IReadOnlyList<T> Items,int Total,int Page,int PageSize);
public sealed record IntegrationRequest(string ProviderType,string IntegrationName,string Environment,Dictionary<string,string> Configuration,string Team="Engineering",bool Enabled=true);
public sealed record IntegrationView(Guid Id,string ProviderType,string IntegrationName,string Account,string Environment,string Team,string Status,string Health,bool Enabled,DateTimeOffset LastSync,DateTimeOffset? LastSuccessfulSync,string CreatedBy,bool IsDefault,bool IsDemoMode,bool HasCredentials,Dictionary<string,string> Configuration);
public sealed record ConnectionTestResult(bool Success,string Status,string Message,int ResponseTimeMs,bool IsDemoMode=true);
public sealed record SyncResult(Guid IntegrationId,bool Success,int RecordsProcessed,string Status,string Message,DateTimeOffset StartedAt,DateTimeOffset CompletedAt);
public sealed record AuditEntry(Guid Id,DateTimeOffset Timestamp,string User,string Action,string Provider,string Integration,string Result,string CorrelationId);
public sealed record AlertView(string Id,string Severity,string Title,string Description,string Provider,DateTimeOffset Timestamp);
public sealed class DemoValidationException(string message) : Exception(message);
public sealed class DemoConflictException(string message) : Exception(message);

public sealed class UnifiedUsageMetric
{
    public string Provider { get; set; } = "";
    public string? AccountName { get; set; }
    public DateTime? Timestamp { get; set; }
    public long? RequestCount { get; set; }
    public long? InputTokens { get; set; }
    public long? OutputTokens { get; set; }
    public long? TotalTokens { get; set; }
    public decimal? Cost { get; set; }
    public string? Currency { get; set; }
    public long? SuccessfulRequests { get; set; }
    public long? FailedRequests { get; set; }
    public string? Model { get; set; }
    public string? Status { get; set; }
    public string? ErrorMessage { get; set; }
}

public sealed class IntegrationConfiguration
{
    public string ProviderType { get; set; } = "";
    public string IntegrationName { get; set; } = "";
    public string? BaseUrl { get; set; }
    public string? UsageEndpoint { get; set; }
    public string? ApiKey { get; set; }
    public string? AuthenticationType { get; set; }
    public string? CustomHeaderName { get; set; }
    public string? Environment { get; set; }
}

public sealed record DemoNormalizeRequest(string Provider,string RawJson);
public sealed record DemoNormalizeResponse(string RawProvider,IReadOnlyList<UnifiedUsageMetric> Normalized);

