using Microsoft.AspNetCore.Mvc;
using UnifiedAI.Api.Models;
using UnifiedAI.Api.Services;
namespace UnifiedAI.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class IntegrationsController(IProviderCatalog catalog,RuntimeIntegrationService runtime) : ControllerBase
{
    private string Correlation=>HttpContext.Response.Headers["X-Correlation-ID"].ToString();
    [HttpGet("providers")] public IReadOnlyList<ProviderDefinition> Providers()=>catalog.All;
    [HttpGet("providers/{providerType}")] public ProviderDefinition Provider(string providerType)=>catalog.Get(providerType);
    [HttpGet("providers/{providerType}/configuration-schema")] public ProviderDefinition Schema(string providerType)=>catalog.Get(providerType);
    [HttpGet("integrations")] public IReadOnlyList<IntegrationView> List()=>runtime.List();
    [HttpGet("integrations/{id:guid}")] public IntegrationView Get(Guid id)=>runtime.Get(id);
    [HttpPost("integrations/test")] public ConnectionTestResult Test(IntegrationRequest request)=>runtime.Test(request,Correlation);
    [HttpPost("integrations")] public IActionResult Add(IntegrationRequest request){var view=runtime.Save(request,null,Correlation);return CreatedAtAction(nameof(Get),new{id=view.Id},view);}
    [HttpPut("integrations/{id:guid}")] public IntegrationView Edit(Guid id,IntegrationRequest request)=>runtime.Save(request,id,Correlation);
    [HttpPost("integrations/{id:guid}/test")] public ConnectionTestResult TestExisting(Guid id)=>runtime.TestExisting(id,Correlation);
    [HttpPost("integrations/{id:guid}/sync")] public SyncResult Sync(Guid id)=>runtime.Sync(id,Correlation);
    [HttpGet("integrations/{id:guid}/sync-history")] public IReadOnlyList<SyncResult> History(Guid id)=>runtime.History(id);
    [HttpPatch("integrations/{id:guid}/enabled")] public IntegrationView Toggle(Guid id,[FromBody]ToggleRequest request)=>runtime.Toggle(id,request.Enabled,Correlation);
    [HttpDelete("integrations/{id:guid}")] public IActionResult Delete(Guid id){runtime.Delete(id,Correlation);return NoContent();}
    [HttpGet("audit-logs")] public PageResult<AuditEntry> Audit([FromQuery]string? provider,[FromQuery]string? action,[FromQuery]string? result,[FromQuery]string? user,[FromQuery]DateOnly? from,[FromQuery]DateOnly? to,[FromQuery]int page=1,[FromQuery]int pageSize=25)
    {
        if(page<1||pageSize<1||pageSize>200)throw new DemoValidationException("Invalid pagination.");
        var rows=runtime.Audit().Where(x=>string.IsNullOrEmpty(provider)||x.Provider==provider).Where(x=>string.IsNullOrEmpty(action)||x.Action.Contains(action,StringComparison.OrdinalIgnoreCase)).Where(x=>string.IsNullOrEmpty(result)||x.Result==result).Where(x=>string.IsNullOrEmpty(user)||x.User.Contains(user,StringComparison.OrdinalIgnoreCase)).Where(x=>!from.HasValue||DateOnly.FromDateTime(x.Timestamp.UtcDateTime)>=from).Where(x=>!to.HasValue||DateOnly.FromDateTime(x.Timestamp.UtcDateTime)<=to).ToArray();
        return new(rows.Skip((page-1)*pageSize).Take(pageSize).ToArray(),rows.Length,page,pageSize);
    }
}
public sealed record ToggleRequest(bool Enabled);

