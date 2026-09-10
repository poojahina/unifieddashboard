using Microsoft.AspNetCore.Mvc;
using UnifiedAI.Api.Models;
using UnifiedAI.Api.Services;
namespace UnifiedAI.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class AnalyticsController(IDashboardDataService data) : ControllerBase
{
    [HttpGet("metadata")] public object Metadata()=>data.Metadata();
    [HttpGet("dashboard/summary")] public DashboardSummary Summary([FromQuery]UsageFilter filter)=>data.Summary(filter);
    [HttpGet("dashboard/provider-summary")] public IReadOnlyList<ProviderSummary> Providers([FromQuery]UsageFilter filter)=>data.ProviderSummary(filter);
    [HttpGet("dashboard/top-consumers")] public TopConsumers Consumers([FromQuery]UsageFilter filter)=>data.TopConsumers(filter);
    [HttpGet("usage")] public PageResult<UsageRecord> Usage([FromQuery]UsageFilter filter)=>data.Usage(filter);
    [HttpGet("usage/trends")] public IReadOnlyList<TrendPoint> Trends([FromQuery]UsageFilter filter)=>data.Trends(filter);
    [HttpGet("usage/token-trends")] public object TokenTrends([FromQuery]UsageFilter filter)=>data.Filter(filter).GroupBy(x=>new{x.Date,x.Provider}).Select(g=>new{g.Key.Date,g.Key.Provider,InputTokens=g.Sum(x=>x.InputTokens),OutputTokens=g.Sum(x=>x.OutputTokens),TotalTokens=g.Sum(x=>x.TotalTokens)});
    [HttpGet("usage/request-status")] public object Status([FromQuery]UsageFilter filter)=>data.Filter(filter).GroupBy(x=>x.Provider).Select(g=>new{Provider=g.Key,SuccessfulRequests=g.Sum(x=>x.SuccessfulRequests),FailedRequests=g.Sum(x=>x.FailedRequests),SuccessRate=100d*g.Sum(x=>x.SuccessfulRequests)/g.Sum(x=>x.RequestCount)});
    [HttpGet("costs/trends")] public IReadOnlyList<TrendPoint> CostTrends([FromQuery]UsageFilter filter){filter.Metric="cost";return data.Trends(filter);}
    [HttpGet("costs/summary")] public object Costs([FromQuery]UsageFilter filter)
    {
        var s=data.Summary(filter);var top=data.TopConsumers(filter);
        return new{TotalCost=s.EstimatedCost,PreviousPeriodCost=data.PreviousPeriodCost(filter),PercentageChange=s.CostChangePercentage,CostByProvider=data.ProviderSummary(filter).Select(x=>new{Name=x.DisplayName,Cost=x.EstimatedCost,Requests=x.Requests}),CostByAccount=top.Accounts,CostByModel=top.Models,CostByTeam=top.Teams};
    }
    [HttpGet("health")] public object Health()=>new{Status="Healthy",Mode="Demo",Database="Not used",Persistence="None",ExternalProviderCalls=false};
    [HttpGet("alerts")] public IReadOnlyList<AlertView> Alerts()=>[
        new("service-now-errors","Warning","ServiceNow error rate increased","The fixed September 10 demo observation shows a 3.0% error rate. Review request outcomes in Usage Analytics.","ServiceNow",new(2026,9,10,8,40,0,TimeSpan.Zero)),
        new("cost-growth","Info","Consumption is trending upward","Weekday demand is increasing in the fixed demo dataset. Compare team and account allocation in Cost Analytics.","All providers",new(2026,9,10,8,0,0,TimeSpan.Zero))
    ];
}

