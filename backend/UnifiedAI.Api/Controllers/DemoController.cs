using Microsoft.AspNetCore.Mvc;
using UnifiedAI.Api.Models;
using UnifiedAI.Api.Services;

namespace UnifiedAI.Api.Controllers;

[ApiController]
[Route("api/demo")]
public sealed class DemoController(IntegrationService integrations,IWebHostEnvironment environment) : ControllerBase
{
    [HttpPost("normalize")]
    public async Task<ActionResult<DemoNormalizeResponse>> Normalize(DemoNormalizeRequest request,CancellationToken cancellationToken)
    {
        if(!environment.IsDevelopment()) return NotFound();
        var normalized=await integrations.NormalizeRawAsync(request.Provider,request.RawJson,cancellationToken);
        return new DemoNormalizeResponse(request.Provider,normalized);
    }

    [HttpPost("provider/{providerType}/normalize")]
    public async Task<ActionResult<DemoNormalizeResponse>> NormalizeProvider(string providerType,CancellationToken cancellationToken)
    {
        if(!environment.IsDevelopment()) return NotFound();
        var normalized=await integrations.GetUsageAsync(new IntegrationConfiguration{ProviderType=providerType,IntegrationName=$"{providerType} demo"},cancellationToken);
        return new DemoNormalizeResponse(providerType,normalized);
    }
}
