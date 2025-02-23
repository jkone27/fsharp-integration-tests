namespace Web.CSharp.Test

open System
open Xunit
open Microsoft.AspNetCore.Mvc.Testing
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Http
open System.Net.Http.Json


// TODO: before starting the tests, check that json-server is running in docker if running "e2e" simulation
// docker compose up...


type CustomAppFactory() = 
    // references Web.CSharp.Program partial class
    inherit WebApplicationFactory<Program>()

    // here we configure the clients in the test server
    override this.ConfigureWebHost (builder: IWebHostBuilder): unit = 
        builder.ConfigureServices(fun s -> 
            s.ConfigureHttpClientDefaults(fun c -> 
                c.ConfigureHttpClient(fun cc -> 
                    // testing e2e with docker compose
                    // important, base address MUST end with / in aspnet
                    // thanks - https://www.damirscorner.com/blog/posts/20240802-HttpClientBaseAddressPathMerging.html
                    cc.BaseAddress <- new Uri("http://localhost:3000/", UriKind.Absolute)
                ) |> ignore
            ) |> ignore
        ) |> ignore
        base.ConfigureWebHost(builder)


    // e.g. for auth in test etc, this client is the test client
    override this.ConfigureClient (client: Net.Http.HttpClient): unit = 
        base.ConfigureClient(client)

type LegacyTests(factory: CustomAppFactory) = 
    interface IClassFixture<CustomAppFactory>

    [<Fact>]
    member this.GetHello () = task {

        let c = factory.CreateClient()

        let! hello = c.GetStringAsync("")
        
        Assert.True(hello.Contains("World"))
    }

    [<Fact>]
    member this.GetJohnsAge () = task {

        let c = factory.CreateClient()

        let! john = c.GetFromJsonAsync<{| Age: int |}>("/john")
        
        Assert.Equal(30, john.Age)
    }