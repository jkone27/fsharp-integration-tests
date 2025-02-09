namespace ApiStub.FSharp.Tests

open Xunit
open fsharpintegrationtests
open Microsoft.AspNetCore.Mvc.Testing
open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.Hosting
open SwaggerProvider
open System.Threading.Tasks
open System.Net.Http
open System
open Microsoft.AspNetCore.TestHost
open Microsoft.Extensions.Http
open Microsoft.AspNetCore.Routing.Template
open Microsoft.AspNetCore.Routing.Patterns
open Microsoft.AspNetCore.Routing
open System.Net
open System.Text.Json
open Microsoft.AspNetCore.Http
open System.Net.Http.Json
open Swensen.Unquote
open ApiStub.FSharp.CE
open ApiStub.FSharp.BuilderExtensions
open ApiStub.FSharp.HttpResponseHelpers
open ApiStub.FSharp

type MyOpenapi = OpenApiClientProvider<"swagger.json">

type MutableUri = Stubbery.StubberyCE.MutableUri

type CETests() =

    let testce = new CE.TestClient<Startup>()

    interface IDisposable with
        member this.Dispose() = (testce :> IDisposable).Dispose()

    [<Fact>]
    member this.``GET weather returns a not null Date``() =

        let application = new WebApplicationFactory<Startup>()

        let client = application.CreateClient()

        let typedClient = MyOpenapi.Client(client)

        task {

            let! forecast = typedClient.GetWeatherForecast()

            Assert.NotNull(forecast.[0].Date)
        }


    [<Fact>]
    member this.``test CE works stubbed client had valid http response and inner request with content``() =

        task {

            let testApp = testce { POSTJ "/stub-this-post" {| Ok = "yeah" |} }

            use internalClient: HttpClient =
                testApp.GetFactory().Services.GetRequiredService<HttpClient>()

            // call an endpoint not invoked/used with a body, to check request content
            let! response = internalClient.PostAsJsonAsync("/stub-this-post", {| Test = "hey" |})

            // can be used by client "middleware" in http client factory (delegating handlers / http message handlers)
            Assert.NotNull(response.RequestMessage)

            let! testMessage = response.RequestMessage.Content.ReadFromJsonAsync<{| Test: string |}>()

            Assert.Equal("hey", testMessage.Test)

            // read response and check it matched the stub body

            let! responseObj = response.Content.ReadFromJsonAsync<{| Ok: string |}>()

            Assert.Equal("yeah", responseObj.Ok)
        }


    [<Fact>]
    member this.``test CE works stubbing multiple endpoints``() =

        task {

            let expected = {| Ok = "yeah" |}

            let testApp =
                testce {
                    GETJ "/externalApi" expected
                    POSTJ "/another/anotherApi" {| Test = "hello"; Time = 1 |}
                    POST "/notUsed" (fun _ _ -> "ok" |> R_TEXT)
                    POST "/notUsed2" (fun _ _ -> "ok" |> R_TEXT)
                    POST "/errRoute" (fun _ _ -> R_ERROR HttpStatusCode.NotAcceptable (new StringContent("err")))
                }

            let factory = testApp.GetFactory()

            use internalClient: HttpClient = factory.Services.GetRequiredService<HttpClient>()

            let! internalResponse = internalClient.GetAsync("externalApi")

            // can be used by client middleware
            Assert.NotNull(internalResponse.RequestMessage)

            use client = factory.CreateClient()

            let! r = client.GetAsync("/Hello")

            r.EnsureSuccessStatusCode() |> ignore

            let! rr = r.Content.ReadFromJsonAsync<MyOpenapi.Hello>()

            Assert.Equal(expected.Ok, rr.Ok)
        }

    [<Fact>]
    member this.``test with extension works two clients``() =

        task {

            let expected = {| Ok = "yeah" |}

            let testApp =
                testce {
                    GETJ "/externalApi" expected
                    POST "/another/anotherApi" (fun _ _ -> {| Test = "hello"; Time = 1 |} |> R_JSON)
                    POST "/notUsed" (fun _ _ -> "ok" |> R_TEXT)
                    POST "/notUsed2" (fun _ _ -> "ok" |> R_TEXT)
                    POST "/errRoute" (fun _ _ -> R_ERROR HttpStatusCode.NotAcceptable (new StringContent("err")))
                }

            let factory = testApp.GetFactory()

            let client = factory.CreateClient()

            let! r = client.GetAsync("/Hello")

            r.EnsureSuccessStatusCode() |> ignore

            let! rr = r.Content.ReadFromJsonAsync<MyOpenapi.Hello>()

            Assert.Equal(expected.Ok, rr.Ok)

            let! r2 = client.GetAsync("/Hello")

            r2.EnsureSuccessStatusCode() |> ignore

        }

    [<Fact>]
    member this.``test with swagger gen client for json apis``() =

        task {
            let expected = {| Ok = "yeah" |}

            use testApp =
                testce {
                    GETJ "/notUsed" expected
                    GETJ "/externalApi" expected
                    POSTJ "/another/anotherApi" expected
                }

            let client = testApp.GetFactory().CreateClient()
            let typedClient = new MyOpenapi.Client(client)

            let! r = typedClient.GetHello()

            Assert.Equal(expected.Ok, r.Ok)
        }

    [<Fact>]
    member this.``check custom client override still allowed before``() =

        task {
            let expected = {| Ok = "yeah" |}

            use testApp =
                testce {
                    GETJ "externalApi" expected
                    POSTJ "another/anotherApi" expected
                }

            let factory = testApp.GetFactory()

            let clientFactory = factory.Services.GetRequiredService<IHttpClientFactory>()
            let customClient = clientFactory.CreateClient("anotherApiClient")

            Assert.True(customClient.BaseAddress.ToString().EndsWith("another/"))

            let client = factory.CreateClient()
            let typedClient = new MyOpenapi.Client(client)

            let! r = typedClient.GetHello()

            test <@ expected.Ok = r.Ok @>
        }

    [<Fact>]
    member this.``check custom client override still allowed after``() =

        task {

            let expected = {| Ok = "yeah" |}

            use testApp =
                testce {
                    GETJ "externalApi" expected
                    POSTJ "test/anotherApi" expected
                }

            let factory =
                testApp.GetFactory()
                |> web_configure_test_services (fun t ->
                    t.AddHttpClient(
                        "anotherApiClient",
                        configureClient = (fun c -> c.BaseAddress <- new Uri("http://localhost/test/"))
                    ))

            let clientFactory = factory.Services.GetRequiredService<IHttpClientFactory>()
            let customClient = clientFactory.CreateClient("anotherApiClient")

            Assert.Equal("http://localhost/test/", customClient.BaseAddress.ToString())

            let client = factory.CreateClient()
            let typedClient = new MyOpenapi.Client(client)

            let! r = typedClient.GetHello()

            test <@ expected.Ok = r.Ok @>
        }
