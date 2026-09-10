using UnifiedAI.Api.Data;
using UnifiedAI.Api.Models;
namespace UnifiedAI.Api.Services;

// Runtime state only. Credentials are isolated from public DTOs and discarded on process exit.
public sealed class RuntimeIntegrationService(IProviderCatalog catalog,DemoConnectionTester tester)
{
    private sealed record Entry(IntegrationView View,Dictionary<string,string> Secrets);
    private readonly object gate=new();
    private readonly Dictionary<Guid,Entry> added=[];
    private readonly Dictionary<Guid,IntegrationView?> defaultChanges=[];
    private readonly List<AuditEntry> audit=[
        new(Guid.Parse("20000000-0000-0000-0000-000000000001"),new(2026,9,10,8,42,0,TimeSpan.Zero),"Demo Administrator","Sync completed","Claude","Enterprise-Claude-Prod","Completed","demo-sync-claude"),
        new(Guid.Parse("20000000-0000-0000-0000-000000000002"),new(2026,9,10,8,40,0,TimeSpan.Zero),"System","Elevated error rate","ServiceNow","ServiceNow-Production","Warning","demo-health-servicenow"),
        new(Guid.Parse("20000000-0000-0000-0000-000000000003"),new(2026,9,10,8,38,0,TimeSpan.Zero),"Demo Administrator","Connection tested","OpenAI","OpenAI-Production","Connected","demo-test-openai")
    ];
    private readonly List<SyncResult> history=[];
    private IEnumerable<IntegrationView> Defaults()=>DemoAccounts.All.Select((a,index)=>new IntegrationView(Guid.Parse("10000000-0000-0000-0000-"+(index+1).ToString("D12")),a.Provider,a.Name,a.Name,a.Environment,a.Team,"Connected",a.Provider=="ServiceNow"?"Warning":"Healthy",true,new(2026,9,10,8,42,0,TimeSpan.Zero),new(2026,9,10,8,42,0,TimeSpan.Zero),"Demo Administrator",true,true,false,[]));
    private IEnumerable<IntegrationView> Current()=>Defaults().Select(x=>defaultChanges.TryGetValue(x.Id,out var changed)?changed:x).OfType<IntegrationView>().Concat(added.Values.Select(x=>x.View));
    public IReadOnlyList<IntegrationView> List(){lock(gate)return Current().Select(Clone).ToArray();}
    private static IntegrationView Clone(IntegrationView view)=>view with{Configuration=new(view.Configuration)};
    private IntegrationView Find(Guid id)=>Current().FirstOrDefault(x=>x.Id==id)??throw new KeyNotFoundException("Integration not found.");
    public IntegrationView Get(Guid id){lock(gate)return Clone(Find(id));}
    public IReadOnlyList<AuditEntry> Audit(){lock(gate)return audit.OrderByDescending(x=>x.Timestamp).ToArray();}
    private void Log(string action,IntegrationView view,string result,string correlation)
    {
        audit.Add(new(Guid.NewGuid(),DateTimeOffset.UtcNow,"Demo Administrator",action,view.ProviderType,view.IntegrationName,result,correlation));
        if(audit.Count>1000) audit.RemoveAt(0);
    }
    public ConnectionTestResult Test(IntegrationRequest request,string correlation)
    {
        var result=tester.Test(request.ProviderType,request.Configuration);
        lock(gate) { var p=catalog.Get(request.ProviderType);audit.Add(new(Guid.NewGuid(),DateTimeOffset.UtcNow,"Demo Administrator","Connection tested",p.ProviderType,"Unsaved integration",result.Status,correlation)); if(audit.Count>1000)audit.RemoveAt(0); }
        return result;
    }
    public ConnectionTestResult TestExisting(Guid id,string correlation)
    {
        lock(gate)
        {
            var view=Find(id);
            var values=new Dictionary<string,string>(view.Configuration);
            if(added.TryGetValue(id,out var entry)) foreach(var pair in entry.Secrets) values[pair.Key]=pair.Value;
            var result=view.IsDefault?new ConnectionTestResult(true,"Connected","Default integration validated in Demo Mode.",184):tester.Test(view.ProviderType,values);
            Log("Connection tested",view,result.Status,correlation);return result;
        }
    }
    public IntegrationView Save(IntegrationRequest r,Guid? id,string correlation)
    {
        lock(gate)
        {
            var schema=catalog.Get(r.ProviderType);var name=r.IntegrationName?.Trim()??"";
            if(name.Length is <1 or >120||name.Any(char.IsControl)) throw new DemoValidationException("Enter an integration name of 1–120 characters.");
            if(!new[]{"Production","Development","Test","Staging"}.Contains(r.Environment)) throw new DemoValidationException("Select a valid environment.");
            if(string.IsNullOrWhiteSpace(r.Team)||r.Team.Length>120) throw new DemoValidationException("Enter a team of 1–120 characters.");
            if(added.Count>=100&&!id.HasValue) throw new DemoConflictException("The demo supports up to 100 runtime integrations.");
            var old=id.HasValue?Find(id.Value):null;
            if(old!=null&&old.ProviderType!=schema.ProviderType) throw new DemoValidationException("An existing integration's provider cannot be changed.");
            if(Current().Any(x=>x.Id!=id&&x.IntegrationName.Equals(name,StringComparison.OrdinalIgnoreCase))) throw new DemoConflictException("An integration with that name already exists.");
            var config=new Dictionary<string,string>(r.Configuration);
            if(id.HasValue&&added.TryGetValue(id.Value,out var previous))
                foreach(var pair in previous.Secrets) if(!config.TryGetValue(pair.Key,out var value)||string.IsNullOrEmpty(value)) config[pair.Key]=pair.Value;
            var result=tester.Test(schema.ProviderType,config);
            if(!result.Success) throw new DemoValidationException(result.Message);
            var secretNames=schema.Fields.Where(x=>x.Secret).Select(x=>x.Name).ToHashSet();
            var publicValues=config.Where(x=>!secretNames.Contains(x.Key)).ToDictionary();
            var secretValues=config.Where(x=>secretNames.Contains(x.Key)).ToDictionary();
            var view=new IntegrationView(id??Guid.NewGuid(),schema.ProviderType,name,name,r.Environment,r.Team,r.Enabled?"Connected":"Disabled","Healthy",r.Enabled,old?.LastSync??DateTimeOffset.MinValue,old?.LastSuccessfulSync,old?.CreatedBy??"Demo Administrator",old?.IsDefault??false,true,secretValues.Count>0,publicValues);
            if(old?.IsDefault==true) defaultChanges[old.Id]=null;
            added[view.Id]=new(view,secretValues);
            Log(old==null?"Integration created":"Integration updated",view,"Success",correlation);
            return Clone(view);
        }
    }
    public IntegrationView Toggle(Guid id,bool enabled,string correlation)
    {
        lock(gate)
        {
            var view=Find(id) with{Enabled=enabled,Status=enabled?"Connected":"Disabled"};
            StoreView(view);Log(enabled?"Integration enabled":"Integration disabled",view,"Success",correlation);return Clone(view);
        }
    }
    private void StoreView(IntegrationView view) {if(added.TryGetValue(view.Id,out var e))added[view.Id]=e with{View=view};else defaultChanges[view.Id]=view;}
    public void Delete(Guid id,string correlation)
    {
        lock(gate){var view=Find(id);added.Remove(id);if(view.IsDefault)defaultChanges[id]=null;Log("Integration deleted",view,"Success",correlation);}
    }
    public SyncResult Sync(Guid id,string correlation)
    {
        lock(gate)
        {
            var view=Find(id);if(!view.Enabled)throw new DemoConflictException("Enable this integration before synchronizing.");
            var now=DateTimeOffset.UtcNow;
            var result=new SyncResult(id,true,view.IsDefault?1250:0,"Completed","Demo synchronization completed. The fixed historical dataset is unchanged.",now,now);
            history.Add(result);if(history.Count>1000)history.RemoveAt(0);
            StoreView(view with{LastSync=now,LastSuccessfulSync=now,Health="Healthy"});Log("Sync completed",view,"Completed",correlation);return result;
        }
    }
    public IReadOnlyList<SyncResult> History(Guid id)
    {
        lock(gate)
        {
            var view=Find(id);var rows=history.Where(x=>x.IntegrationId==id).OrderByDescending(x=>x.CompletedAt).ToList();
            if(view.IsDefault)rows.Add(new(id,true,1250,"Completed","Historical demo sync.",new(2026,9,10,8,41,0,TimeSpan.Zero),new(2026,9,10,8,42,0,TimeSpan.Zero)));
            return rows;
        }
    }
}

