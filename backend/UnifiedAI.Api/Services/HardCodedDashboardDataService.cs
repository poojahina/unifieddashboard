using UnifiedAI.Api.Data;
using UnifiedAI.Api.Models;
namespace UnifiedAI.Api.Services;

public interface IDashboardDataService
{
    DashboardSummary Summary(UsageFilter filter);
    IReadOnlyList<ProviderSummary> ProviderSummary(UsageFilter filter);
    IReadOnlyList<TrendPoint> Trends(UsageFilter filter);
    PageResult<UsageRecord> Usage(UsageFilter filter);
    TopConsumers TopConsumers(UsageFilter filter);
    IReadOnlyList<UsageRecord> Filter(UsageFilter filter);
    object Metadata();
    decimal? PreviousPeriodCost(UsageFilter filter);
}
public sealed class HardCodedDashboardDataService(IProviderCatalog catalog) : IDashboardDataService
{
    public object Metadata()=>new{IsDemoMode=true,DefaultFrom=DemoUsageData.DefaultFrom,DefaultTo=DemoUsageData.DefaultTo,Accounts=DemoAccounts.All,Environments=DemoAccounts.All.Select(x=>x.Environment).Distinct(),Teams=DemoAccounts.All.Select(x=>x.Team).Distinct(),Models=DemoAccounts.All.Select(x=>x.Model).Distinct(),ComparisonNote="Previous-period comparisons use fixed monthly baseline fixtures; custom periods may have no comparable baseline."};
    private static (DateOnly From,DateOnly To) Period(UsageFilter f)
    {
        var from=f.From??DemoUsageData.DefaultFrom; var to=f.To??DemoUsageData.DefaultTo;
        if(from>to||to.DayNumber-from.DayNumber>366) throw new DemoValidationException("Select a valid date range of at most 366 days.");
        return(from,to);
    }
    public IReadOnlyList<UsageRecord> Filter(UsageFilter f)
    {
        var (from,to)=Period(f);
        return DemoUsageData.All.Where(x=>x.Date>=from&&x.Date<=to)
            .Where(x=>string.IsNullOrEmpty(f.Provider)||x.Provider==f.Provider)
            .Where(x=>string.IsNullOrEmpty(f.Account)||x.AccountId==f.Account)
            .Where(x=>string.IsNullOrEmpty(f.Environment)||x.Environment==f.Environment)
            .Where(x=>string.IsNullOrEmpty(f.Team)||x.Team==f.Team)
            .Where(x=>string.IsNullOrEmpty(f.Model)||x.Model==f.Model).ToArray();
    }
    public decimal? PreviousPeriodCost(UsageFilter f)
    {
        var (from,to)=Period(f);var days=to.DayNumber-from.DayNumber+1;
        var previous=Filter(new(){From=from.AddDays(-days),To=from.AddDays(-1),Provider=f.Provider,Account=f.Account,Environment=f.Environment,Team=f.Team,Model=f.Model});
        return previous.Count==0?null:previous.Sum(x=>x.Cost);
    }
    public static double? Change(decimal current,decimal previous)=>previous==0?null:Math.Round((double)((current-previous)/previous*100),2);
    private static long? Tokens(IEnumerable<UsageRecord> rows,Func<UsageRecord,long?> get)=>rows.Any(x=>get(x)!=null)?rows.Sum(get):null;
    public DashboardSummary Summary(UsageFilter f)
    {
        var a=Filter(f);var (from,to)=Period(f);var days=to.DayNumber-from.DayNumber+1;
        var b=Filter(new(){From=from.AddDays(-days),To=from.AddDays(-1),Provider=f.Provider,Account=f.Account,Environment=f.Environment,Team=f.Team,Model=f.Model});
        var requests=a.Sum(x=>x.RequestCount); var errors=a.Sum(x=>x.FailedRequests);
        var previousRequests=b.Sum(x=>x.RequestCount);
        var success=requests==0?0:100d*(requests-errors)/requests;
        var previousSuccess=previousRequests==0?0:100d*b.Sum(x=>x.SuccessfulRequests)/previousRequests;
        return new(requests,Tokens(a,x=>x.InputTokens),Tokens(a,x=>x.OutputTokens),Tokens(a,x=>x.TotalTokens),a.Count==0?null:a.Sum(x=>x.Cost),success,a.Select(x=>x.Provider).Distinct().Count(),a.Select(x=>x.AccountId).Distinct().Count(),Change(requests,previousRequests),Change(a.Sum(x=>x.TotalTokens)??0,b.Sum(x=>x.TotalTokens)??0),Change(a.Sum(x=>x.Cost)??0,b.Sum(x=>x.Cost)??0),requests==0?0:100d*errors/requests,Change(a.Sum(x=>x.InputTokens)??0,b.Sum(x=>x.InputTokens)??0),Change(a.Sum(x=>x.OutputTokens)??0,b.Sum(x=>x.OutputTokens)??0),Change(a.Select(x=>x.Provider).Distinct().Count(),b.Select(x=>x.Provider).Distinct().Count()),Change(a.Select(x=>x.AccountId).Distinct().Count(),b.Select(x=>x.AccountId).Distinct().Count()),Change((decimal)success,(decimal)previousSuccess));
    }
    public IReadOnlyList<ProviderSummary> ProviderSummary(UsageFilter f)=>Filter(f).GroupBy(x=>x.Provider).Select(g=>{
        var p=catalog.Get(g.Key);var requests=g.Sum(x=>x.RequestCount);var failures=g.Sum(x=>x.FailedRequests);
        return new ProviderSummary(p.ProviderType,p.DisplayName,p.Color,p.Monogram,"Demo connected",p.ProviderType=="ServiceNow"?"Warning":"Healthy",requests,Tokens(g,x=>x.InputTokens),Tokens(g,x=>x.OutputTokens),Tokens(g,x=>x.TotalTokens),g.Sum(x=>x.Cost),requests==0?0:100d*(requests-failures)/requests,failures,g.Select(x=>x.AccountId).Distinct().Count(),new(2026,9,10,8,42,0,TimeSpan.Zero));
    }).ToArray();
    public static decimal? Metric(UsageRecord x,string metric)=>metric switch{"requests"=>x.RequestCount,"inputTokens"=>x.InputTokens,"outputTokens"=>x.OutputTokens,"totalTokens"=>x.TotalTokens,"cost"=>x.Cost,"errors"=>x.FailedRequests,_=>throw new DemoValidationException("Unsupported metric.")};
    public IReadOnlyList<TrendPoint> Trends(UsageFilter f)
    {
        if(!new[]{"requests","inputTokens","outputTokens","totalTokens","cost","errors"}.Contains(f.Metric)) throw new DemoValidationException("Unsupported metric.");
        if(!new[]{"daily","weekly","monthly"}.Contains(f.Granularity)) throw new DemoValidationException("Choose daily, weekly or monthly granularity.");
        DateOnly Bucket(DateOnly d)=>f.Granularity switch{"weekly"=>d.AddDays(-((int)d.DayOfWeek+6)%7),"monthly"=>new(d.Year,d.Month,1),_=>d};
        return Filter(f).GroupBy(x=>new{Date=Bucket(x.Date),x.Provider}).Select(g=>new TrendPoint(g.Key.Date,g.Key.Provider,g.Any(x=>Metric(x,f.Metric)!=null)?g.Sum(x=>Metric(x,f.Metric)):null)).OrderBy(x=>x.Date).ToArray();
    }
    public PageResult<UsageRecord> Usage(UsageFilter f)
    {
        if(f.Page<1||f.PageSize<1||f.PageSize>200) throw new DemoValidationException("Page must be positive; pageSize must be between 1 and 200.");
        if(!new[]{"asc","desc"}.Contains(f.SortDirection)) throw new DemoValidationException("Sort direction must be asc or desc.");
        var rows=Filter(f);Func<UsageRecord,IComparable?> key=f.SortBy switch{"date"=>x=>x.Date,"requests"=>x=>x.RequestCount,"cost"=>x=>x.Cost,"provider"=>x=>x.Provider,_=>throw new DemoValidationException("Unsupported sort field.")};
        var sorted=f.SortDirection=="desc"?rows.OrderByDescending(key):rows.OrderBy(key);
        return new(sorted.ThenBy(x=>x.Id).Skip((f.Page-1)*f.PageSize).Take(f.PageSize).ToArray(),rows.Count,f.Page,f.PageSize);
    }
    public TopConsumers TopConsumers(UsageFilter f)
    {
        var rows=Filter(f);
        IReadOnlyList<Consumer> Group(Func<UsageRecord,string> key)=>rows.GroupBy(key).Select(g=>new Consumer(g.Key,g.Sum(x=>x.RequestCount),Tokens(g,x=>x.TotalTokens),g.Sum(x=>x.Cost))).OrderByDescending(x=>x.Cost).ToArray();
        return new(Group(x=>x.AccountName),Group(x=>x.Model),Group(x=>x.Team),Group(x=>x.Environment));
    }
}

