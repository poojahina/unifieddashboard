using System.Text.Json;
using UnifiedAI.Api.Models;
namespace UnifiedAI.Api.Services;

public sealed class DemoConnectionTester(IProviderCatalog catalog)
{
    public ConnectionTestResult Test(string providerType,IReadOnlyDictionary<string,string> config)
    {
        var schema=catalog.Get(providerType);
        if(config.Count>40||config.Any(x=>x.Value is null||x.Value.Length>16000)) throw new DemoValidationException("Configuration is too large or contains invalid values.");
        if(config.Keys.Except(schema.Fields.Select(x=>x.Name)).Any()) throw new DemoValidationException("Unknown configuration field.");
        foreach(var f in schema.Fields)
        {
            var value=config.GetValueOrDefault(f.Name,"").Trim();
            var required=f.Required||(f.RequiredWhenField!=null&&f.RequiredWhenValues!.Contains(config.GetValueOrDefault(f.RequiredWhenField,"")));
            if(required&&value.Length==0) return new(false,"MissingCredentials",f.Label+" is required.",24);
            if(f.Type=="url"&&value.Length>0&&(!Uri.TryCreate(value,UriKind.Absolute,out var uri)||uri.Scheme!="https"||!string.IsNullOrEmpty(uri.UserInfo)||!string.IsNullOrEmpty(uri.Query)||!string.IsNullOrEmpty(uri.Fragment))) return new(false,"InvalidEndpoint","Use an HTTPS endpoint without user information, query parameters, or fragments. Put query parameters in the protected configuration field.",32);
            if(f.Options!=null&&value.Length>0&&!f.Options.Contains(value)) return new(false,"InvalidConfiguration","Select a valid "+f.Label+".",25);
            if(f.Type=="textarea"&&value.Length>0)
            {
                try { using var json=JsonDocument.Parse(value); if(json.RootElement.ValueKind!=JsonValueKind.Object) return new(false,"InvalidConfiguration",f.Label+" must be a JSON object.",25); }
                catch(JsonException) { return new(false,"InvalidConfiguration",f.Label+" must contain valid JSON.",25); }
            }
        }
        if(config.Values.Any(v=>v=="invalid-key")) return new(false,"AuthenticationFailed","The demo credential was rejected. Try a different dummy value.",184);
        if(config.Values.Any(v=>v=="rate-limited")) return new(false,"RateLimited","Simulated provider rate limit. Try another dummy credential.",184);
        if(config.Values.Any(v=>v=="unavailable")) return new(false,"ProviderUnavailable","The demo provider is temporarily unavailable.",184);
        return new(true,"Connected","Connection validated successfully in Demo Mode. No external request was made.",184);
    }
}

