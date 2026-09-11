using System.Diagnostics;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using UnifiedAI.Api.Connectors;
using UnifiedAI.Api.Models;
using UnifiedAI.Api.Services;

var builder=WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o=>o.SwaggerDoc("v1",new(){Title="Unified AI Consumption Dashboard",Version="v1",Description="POC: provider connectors return raw JSON, the normalizer produces UnifiedUsageMetric, and dashboard APIs expose the common model. No database or persistence."}));
builder.Services.AddSingleton<IProviderCatalog,ProviderCatalog>();
builder.Services.AddSingleton<IDashboardDataService,HardCodedDashboardDataService>();
builder.Services.AddSingleton<DemoConnectionTester>();
builder.Services.AddSingleton<RuntimeIntegrationService>();
builder.Services.AddSingleton<ClaudeConnector>();
builder.Services.AddSingleton<OpenAIConnector>();
builder.Services.AddSingleton<CopilotConnector>();
builder.Services.AddSingleton<ServiceNowConnector>();
builder.Services.AddSingleton<GenericRestConnector>();
builder.Services.AddSingleton<IProviderConnector>(sp=>sp.GetRequiredService<ClaudeConnector>());
builder.Services.AddSingleton<IProviderConnector>(sp=>sp.GetRequiredService<OpenAIConnector>());
builder.Services.AddSingleton<IProviderConnector>(sp=>sp.GetRequiredService<CopilotConnector>());
builder.Services.AddSingleton<IProviderConnector>(sp=>sp.GetRequiredService<ServiceNowConnector>());
builder.Services.AddSingleton<ConnectorResolver>();
builder.Services.AddSingleton<JsonSanitizer>();
builder.Services.AddSingleton<ILlmClient,DemoLlmClient>();
builder.Services.AddSingleton<IUsageNormalizer,LlmUsageNormalizer>();
builder.Services.AddSingleton<UsageValidator>();
builder.Services.AddSingleton<IntegrationService>();
builder.Services.AddHttpClient();
builder.Services.AddCors(o=>o.AddDefaultPolicy(p=>p.WithOrigins(builder.Configuration["FrontendOrigin"]??"http://localhost:4200").AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddRateLimiter(o=>{
    o.RejectionStatusCode=429;
    o.AddFixedWindowLimiter("api",options=>{options.PermitLimit=300;options.Window=TimeSpan.FromMinutes(1);options.QueueLimit=0;});
});
var app=builder.Build();
app.Use(async(context,next)=>{
    var correlation=Activity.Current?.TraceId.ToString()??Guid.NewGuid().ToString("N");
    context.Response.Headers["X-Correlation-ID"]=correlation;
    context.Response.Headers["X-Content-Type-Options"]="nosniff";
    context.Response.Headers["X-Frame-Options"]="DENY";
    context.Response.Headers["Referrer-Policy"]="no-referrer";
    context.Response.Headers["Cache-Control"]="no-store";
    try {await next();}
    catch(Exception ex)
    {
        var (status,message)=ex switch {
            DemoValidationException=>(400,ex.Message),DemoConflictException=>(409,ex.Message),
            KeyNotFoundException=>(404,ex.Message),BadHttpRequestException=>(400,"Invalid request."),
            _=>(500,"The request could not be completed. Please retry.")
        };
        if(status==500)app.Logger.LogError("Request failed; correlation {CorrelationId}; exception type {ExceptionType}",correlation,ex.GetType().Name);
        context.Response.StatusCode=status;
        await context.Response.WriteAsJsonAsync(new{Title=message,Status=status,CorrelationId=correlation});
    }
});
app.UseCors();
app.UseRateLimiter();
app.UseSwagger();
app.UseSwaggerUI(o=>{o.SwaggerEndpoint("/swagger/v1/swagger.json","Unified AI POC v1");o.DocumentTitle="Unified AI API Explorer";});
app.MapControllers().RequireRateLimiting("api");
app.Run();
public partial class Program { }

