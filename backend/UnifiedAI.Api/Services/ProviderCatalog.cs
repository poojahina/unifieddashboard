using UnifiedAI.Api.Models;
namespace UnifiedAI.Api.Services;

public interface IProviderCatalog { IReadOnlyList<ProviderDefinition> All {get;} ProviderDefinition Get(string type); }
public sealed class ProviderCatalog : IProviderCatalog
{
    private static ConfigurationField F(string n,string l,string t="text",bool required=false,bool secret=false,string[]? options=null,string? hint=null,string? when=null,string[]? values=null)=>new(n,l,t,required,secret,options,hint,when,values);
    private static Dictionary<string,string> Cap()=>new(){["requests"]="supported",["inputTokens"]="supported",["outputTokens"]="supported",["totalTokens"]="supported",["cost"]="supported",["models"]="supported",["accounts"]="supported",["errors"]="supported",["users"]="unsupported"};
    public IReadOnlyList<ProviderDefinition> All {get;}=[
        new("Claude","Anthropic Claude","#b48768","A","Enterprise language models",Cap(),[
            F("adminApiKey","Admin API key","password",true,true,hint:"Use any dummy value. invalid-key simulates authentication failure."),
            F("organizationId","Organization ID"),F("baseEndpoint","Base endpoint","url",hint:"Optional HTTPS endpoint. No external calls are made.")]),
        new("OpenAI","OpenAI","#438a78","O","Models and API platform",Cap(),[
            F("adminApiKey","Admin API key / API key","password",true,true),F("organizationId","Organization ID"),F("accountId","Account ID"),F("baseEndpoint","Base endpoint","url")]),
        new("Copilot","Microsoft Copilot","#637dc5","M","Workplace AI assistance",Cap(),[
            F("tenantId","Tenant ID","text",true),F("clientId","Client ID","text",true),F("clientSecret","Client secret","password",true,true),F("baseEndpoint","Base endpoint","url")]),
        new("ServiceNow","ServiceNow","#819866","S","Enterprise workflow intelligence",Cap(),[
            F("baseEndpoint","Instance URL","url",true),F("authenticationType","Authentication type","dropdown",true,options:["Basic","OAuth2"]),
            F("username","Username",when:"authenticationType",values:["Basic"]),F("password","Password","password",secret:true,when:"authenticationType",values:["Basic"]),
            F("clientId","Client ID",when:"authenticationType",values:["OAuth2"]),F("clientSecret","Client secret","password",secret:true,when:"authenticationType",values:["OAuth2"])]),
        new("CustomREST","Custom REST API","#9279b0","R","Configure another AI or SaaS provider",Cap(),[
            F("providerName","Provider name","text",true),F("baseEndpoint","Base endpoint","url",true),F("usageEndpoint","Usage endpoint path","text",true,hint:"For example /v1/usage. Configuration only in this POC."),
            F("authenticationType","Authentication type","dropdown",true,options:["ApiKey","Bearer","Basic","OAuth2","CustomHeader"]),
            F("apiKey","API key / admin key / token","password",secret:true,when:"authenticationType",values:["ApiKey","Bearer","CustomHeader"]),
            F("authorizationHeader","Authorization header",when:"authenticationType",values:["ApiKey","CustomHeader"]),
            F("authorizationScheme","Authorization scheme"),F("username","Username",when:"authenticationType",values:["Basic"]),
            F("password","Password","password",secret:true,when:"authenticationType",values:["Basic"]),F("clientId","Client ID",when:"authenticationType",values:["OAuth2"]),
            F("clientSecret","Client secret","password",secret:true,when:"authenticationType",values:["OAuth2"]),F("tokenEndpoint","Token endpoint","url",when:"authenticationType",values:["OAuth2"]),
            F("region","Region"),F("httpMethod","HTTP method","dropdown",true,options:["GET","POST"]),
            F("customHeaders","Custom headers (JSON)","textarea",secret:true,hint:"Treated as secret; never returned by the API."),
            F("queryParameters","Query parameters (JSON)","textarea",secret:true,hint:"Treated as secret because parameters may contain credentials."),
            F("responseMapping","Response mapping (JSON)","textarea",true,hint:"Example: {\"requests\":\"data.requests\",\"timestamp\":\"data.date\"}")])
    ];
    public ProviderDefinition Get(string type)=>All.FirstOrDefault(p=>p.ProviderType.Equals(type,StringComparison.OrdinalIgnoreCase))??throw new DemoValidationException("Unknown provider type.");
}

