using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using UnifiedAI.Api.Models;
using UnifiedAI.Api.Services;
using Xunit;
namespace UnifiedAI.Tests;
public class IntegrationTests
{
    private static IntegrationRequest Request(string name="POC test")=>new("Claude",name,"Production",new(){["adminApiKey"]="dummy-secret-test-value"},"Engineering");
    [Theory]
    [InlineData("invalid-key","AuthenticationFailed")]
    [InlineData("rate-limited","RateLimited")]
    [InlineData("unavailable","ProviderUnavailable")]
    [InlineData("","MissingCredentials")]
    [InlineData("dummy-value","Connected")]
    public void ConnectionSimulation(string key,string status)
    {
        var tester=new DemoConnectionTester(new ProviderCatalog());var result=tester.Test("Claude",new Dictionary<string,string>{{"adminApiKey",key}});
        Assert.Equal(status,result.Status);Assert.True(result.IsDemoMode);
    }
    [Fact] public void ConditionalAuthenticationAndEndpointsAreValidated()
    {
        var tester=new DemoConnectionTester(new ProviderCatalog());
        Assert.Equal("InvalidEndpoint",tester.Test("Claude",new Dictionary<string,string>{{"adminApiKey","x"},{"baseEndpoint","http://example.com"}}).Status);
        Assert.Equal("MissingCredentials",tester.Test("ServiceNow",new Dictionary<string,string>{{"baseEndpoint","https://demo.example.com"},{"authenticationType","Basic"}}).Status);
        Assert.True(tester.Test("ServiceNow",new Dictionary<string,string>{{"baseEndpoint","https://demo.example.com"},{"authenticationType","Basic"},{"username","demo"},{"password","demo"}}).Success);
    }
    [Fact] public void EndpointsCannotSmuggleCredentialsIntoPublicConfiguration()
    {
        var tester=new DemoConnectionTester(new ProviderCatalog());
        Assert.Equal("InvalidEndpoint",tester.Test("Claude",new Dictionary<string,string>{{"adminApiKey","demo"},{"baseEndpoint","https://example.com?api_key=secret"}}).Status);
    }
    [Fact] public void RuntimeLifecycleDoesNotModifyConsumptionOrExposeSecrets()
    {
        var catalog=new ProviderCatalog();var runtime=new RuntimeIntegrationService(catalog,new(catalog));
        var before=new HardCodedDashboardDataService(catalog).Summary(new());
        var saved=runtime.Save(Request(),null,"test");
        Assert.True(saved.HasCredentials);Assert.DoesNotContain("dummy-secret-test-value",JsonSerializer.Serialize(runtime.List()));
        Assert.True(runtime.TestExisting(saved.Id,"test").Success);
        var sync=runtime.Sync(saved.Id,"test");Assert.Equal(0,sync.RecordsProcessed);
        Assert.Single(runtime.History(saved.Id));
        Assert.Equal(before,new HardCodedDashboardDataService(catalog).Summary(new()));
        runtime.Toggle(saved.Id,false,"test");Assert.Throws<DemoConflictException>(()=>runtime.Sync(saved.Id,"test"));
        runtime.Delete(saved.Id,"test");Assert.Throws<KeyNotFoundException>(()=>runtime.Get(saved.Id));
        Assert.DoesNotContain("dummy-secret-test-value",JsonSerializer.Serialize(runtime.Audit()));
    }
    [Fact] public void DuplicateNamesRejectedAndNewProcessStateResets()
    {
        var c=new ProviderCatalog();var a=new RuntimeIntegrationService(c,new(c));a.Save(Request(),null,"test");
        Assert.Throws<DemoConflictException>(()=>a.Save(Request(),null,"test"));
        var b=new RuntimeIntegrationService(c,new(c));Assert.Equal(6,b.List().Count);Assert.Equal(7,a.List().Count);
    }
    [Theory]
    [InlineData("/api/dashboard/summary")][InlineData("/api/dashboard/provider-summary")][InlineData("/api/dashboard/top-consumers")]
    [InlineData("/api/usage")][InlineData("/api/usage/trends")][InlineData("/api/usage/token-trends")][InlineData("/api/usage/request-status")]
    [InlineData("/api/costs/summary")][InlineData("/api/costs/trends")][InlineData("/api/providers")][InlineData("/api/providers/Claude/configuration-schema")]
    [InlineData("/api/integrations")][InlineData("/api/audit-logs")][InlineData("/api/health")][InlineData("/swagger/v1/swagger.json")]
    public async Task ApiEndpointsRespond(string path)
    {
        await using var factory=new WebApplicationFactory<Program>();var client=factory.CreateClient();
        var response=await client.GetAsync(path);Assert.Equal(HttpStatusCode.OK,response.StatusCode);
        Assert.NotEmpty(await response.Content.ReadAsStringAsync());
    }
    [Fact] public async Task ApiCreateTestSyncDeleteAndValidation()
    {
        await using var factory=new WebApplicationFactory<Program>();var client=factory.CreateClient();
        var bad=await client.GetAsync("/api/usage?pageSize=500");Assert.Equal(HttpStatusCode.BadRequest,bad.StatusCode);
        var response=await client.PostAsJsonAsync("/api/integrations",Request());
        Assert.Equal(HttpStatusCode.Created,response.StatusCode);Assert.NotNull(response.Headers.Location);
        var raw=await response.Content.ReadAsStringAsync();Assert.DoesNotContain("dummy-secret-test-value",raw);
        var integration=await response.Content.ReadFromJsonAsync<IntegrationView>();Assert.NotNull(integration);
        Assert.Equal(HttpStatusCode.OK,(await client.PostAsJsonAsync($"/api/integrations/{integration.Id}/sync",new{})).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,(await client.DeleteAsync($"/api/integrations/{integration.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await client.GetAsync($"/api/integrations/{integration.Id}")).StatusCode);
    }
    [Theory]
    [InlineData("Claude",430250,8450000,3250000,11700000)]
    [InlineData("OpenAI",385800,6750000,2650000,9400000)]
    [InlineData("Copilot",248600,4150000,1860000,6010000)]
    [InlineData("ServiceNow",142900,2190000,960000,3150000)]
    public async Task DemoProviderNormalizationUsesCommonMetric(string provider,long requests,long input,long output,long total)
    {
        await using var factory=new WebApplicationFactory<Program>().WithWebHostBuilder(b=>b.UseEnvironment("Development"));
        var client=factory.CreateClient();
        var response=await client.PostAsJsonAsync($"/api/demo/provider/{provider}/normalize",new{});
        Assert.Equal(HttpStatusCode.OK,response.StatusCode);
        var body=await response.Content.ReadFromJsonAsync<DemoNormalizeResponse>();
        Assert.NotNull(body);
        var metric=Assert.Single(body.Normalized);
        Assert.Equal(provider,metric.Provider);
        Assert.Equal(requests,metric.RequestCount);
        Assert.Equal(input,metric.InputTokens);
        Assert.Equal(output,metric.OutputTokens);
        Assert.Equal(total,metric.TotalTokens);
    }
    [Fact] public async Task DemoNormalizeSanitizesSecretsBeforeNormalization()
    {
        await using var factory=new WebApplicationFactory<Program>().WithWebHostBuilder(b=>b.UseEnvironment("Development"));
        var client=factory.CreateClient();
        var request=new DemoNormalizeRequest("UnknownProvider","{\"account\":\"custom-prod\",\"apiKey\":\"secret\",\"usageStats\":{\"totalRequests\":100,\"promptTokens\":5000,\"completionTokens\":2000,\"successful\":96,\"failed\":4}}");
        var response=await client.PostAsJsonAsync("/api/demo/normalize",request);
        Assert.Equal(HttpStatusCode.OK,response.StatusCode);
        var body=await response.Content.ReadFromJsonAsync<DemoNormalizeResponse>();
        Assert.NotNull(body);
        var metric=Assert.Single(body.Normalized);
        Assert.Equal(7000,metric.TotalTokens);
        Assert.Equal("custom-prod",metric.AccountName);
    }
}

