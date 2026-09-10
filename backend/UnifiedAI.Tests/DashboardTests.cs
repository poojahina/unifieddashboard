using UnifiedAI.Api.Data;
using UnifiedAI.Api.Models;
using UnifiedAI.Api.Services;
using Xunit;
namespace UnifiedAI.Tests;
public class DashboardTests
{
    private readonly HardCodedDashboardDataService service=new(new ProviderCatalog());
    [Fact] public void SummaryReconcilesWithProvidersAndTrend()
    {
        var f=new UsageFilter();var summary=service.Summary(f);var providers=service.ProviderSummary(f);
        Assert.Equal(4,summary.ActiveProviders);Assert.Equal(6,summary.ConnectedAccounts);
        Assert.Equal(1454490,summary.TotalRequests);Assert.Equal(36875275,summary.TotalTokens);Assert.Equal(5023m,summary.EstimatedCost);
        Assert.Equal(summary.TotalRequests,providers.Sum(p=>p.Requests));
        Assert.Equal(summary.TotalTokens,summary.InputTokens+summary.OutputTokens);
        Assert.Equal((decimal)summary.TotalRequests,service.Trends(f).Sum(x=>x.Value));
        Assert.Equal(120,service.Trends(f).Count);
    }
    [Fact] public void AccountAndProviderFiltersIntersect()
    {
        var a=service.Filter(new(){Account="claude-dev"});
        Assert.NotEmpty(a);Assert.All(a,x=>{Assert.Equal("Claude",x.Provider);Assert.Equal("Development",x.Environment);Assert.Equal("AI COE",x.Team);});
        Assert.Empty(service.Filter(new(){Provider="OpenAI",Account="claude-dev"}));
    }
    [Fact] public void InclusiveDatesAndModelFiltersWork()
    {
        var records=service.Filter(new(){From=new(2026,9,10),To=new(2026,9,10),Model="Now Assist"});
        Assert.Single(records);Assert.Equal(8260,records[0].RequestCount);Assert.Equal(248,records[0].FailedRequests);
    }
    [Fact] public void EmptyRangeHasNoManufacturedTotals()
    {
        var s=service.Summary(new(){From=new(2027,1,1),To=new(2027,1,31)});
        Assert.Equal(0,s.TotalRequests);Assert.Null(s.TotalTokens);Assert.Null(s.EstimatedCost);Assert.Null(s.RequestChangePercentage);
    }
    [Fact] public void InvalidDatesAreRejected()=>Assert.Throws<DemoValidationException>(()=>service.Summary(new(){From=new(2026,9,10),To=new(2026,8,12)}));
    [Fact] public void CostTrendsAndConsumersReconcile()
    {
        var f=new UsageFilter{Metric="cost"};var sum=service.Summary(f);
        Assert.Equal(sum.EstimatedCost,service.Trends(f).Sum(x=>x.Value));
        Assert.Equal(sum.EstimatedCost,service.TopConsumers(f).Accounts.Sum(x=>x.Cost));
        Assert.Equal(sum.EstimatedCost,service.TopConsumers(f).Models.Sum(x=>x.Cost));
        Assert.Equal(sum.EstimatedCost,service.TopConsumers(f).Teams.Sum(x=>x.Cost));
    }
    [Fact] public void GranularityPreservesTotals()
    {
        Assert.Equal(service.Trends(new()).Sum(x=>x.Value),service.Trends(new(){Granularity="weekly"}).Sum(x=>x.Value));
        Assert.Equal(service.Trends(new()).Sum(x=>x.Value),service.Trends(new(){Granularity="monthly"}).Sum(x=>x.Value));
    }
    [Fact] public void PaginationAndSortingWork()
    {
        var a=service.Usage(new(){Page=1,PageSize=10,SortBy="cost"});var b=service.Usage(new(){Page=2,PageSize=10,SortBy="cost"});
        Assert.Equal(120,a.Total);Assert.Empty(a.Items.Select(x=>x.Id).Intersect(b.Items.Select(x=>x.Id)));
        Assert.True(a.Items.First().Cost>=a.Items.Last().Cost);
        Assert.Throws<DemoValidationException>(()=>service.Usage(new(){PageSize=1000}));
    }
    [Fact] public void PreviousCostIsExactBaseline()=>Assert.Equal(4040.10m,service.PreviousPeriodCost(new()));
    [Fact] public void FixturesAreStableAndValid()
    {
        Assert.Equal(DemoUsageData.All.Count,DemoUsageData.All.Select(x=>x.Id).Distinct().Count());
        Assert.All(DemoUsageData.All,r=>{Assert.True(r.RequestCount>=r.FailedRequests);Assert.Equal(r.RequestCount,r.SuccessfulRequests+r.FailedRequests);Assert.Equal(r.InputTokens+r.OutputTokens,r.TotalTokens);Assert.True(r.Cost>=0);});
    }
}

