namespace Web.Sample.Csharp.Test;

using System;
using Xunit;
using ApiStub.FSharp;
using System.Threading.Tasks;
using static ApiStub.FSharp.CE;
using Web.Sample;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

public class CSharpTests
{
    private static WebApplicationFactory<Web.Sample.Program> getWebAppFactory()
    {
        // create an instance of the test client builder
        var b = new TestClient<Web.Sample.Program>();

        return 
            b.GetJson(b, Clients.Routes.name, new { Name = "Peter" })
            .GetJson(b, Clients.Routes.age, new { Age = 100 })
            .GetFactory();
    }

    // one app factory instance is oke for all tests
    private static readonly WebApplicationFactory<Program> webAppFactory = getWebAppFactory();

    [Fact]
    public async Task CsharpTest_Peter_is_100_years_old()
    {
        var client = webAppFactory.CreateClient();

        var response = await client.PostAsJsonAsync<object>(Services.routeOne, new { });

        Assert.True(response.IsSuccessStatusCode);
    }
}
