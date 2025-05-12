namespace Web.Sample.Csharp.Test;

using System;
using Xunit;
using System.Threading.Tasks;
using Web.Sample;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using static ApiStub.FSharp.CE;
using static ApiStub.Fsharp.CSharp.CsharpExtensions;

public class CSharpTests
{
    private static WebApplicationFactory<Web.Sample.Program> getWebAppFactory() =>
        // create an instance of the test client builder
        new TestWebAppFactoryBuilder<Web.Sample.Program>()
            .GETJ(Clients.Routes.name, new { Name = "Peter" })
            .GETJ(Clients.Routes.age, new { Age = 100 })
            .GetFactory();

    // one app factory instance is oke for all tests
    private static readonly WebApplicationFactory<Program> webAppFactory = getWebAppFactory();

    [Fact]
    public async Task CsharpTest_Peter_is_100_years_old()
    {
        var client = webAppFactory.CreateClient();

        var response = await client.PostAsJsonAsync<object>(Services.routeOne, new { });

        var responseText = await response.Content.ReadAsStringAsync();

        Assert.True(response.IsSuccessStatusCode);
        Assert.Contains("Peter", responseText);
        Assert.Contains("100", responseText);
    }
}
