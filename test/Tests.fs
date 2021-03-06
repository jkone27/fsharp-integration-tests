namespace test

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

open ApiStub.FSharp

type MyOpenapi = OpenApiClientProvider<"swagger.json">

type MutableUri = Stubbery.StubberyCE.MutableUri 

module Tests =

    open CE
    open BuilderExtensions
    open HttpResponseHelpers

    let testce_stubbery () = new Stubbery.StubberyCE.TestStubberyClient<Startup>()
    let testce () = new CE.TestClient<Startup>()

    [<Fact>]
    let ``GET weather returns a not null Date`` () =

        let application = new WebApplicationFactory<Startup>()

        let client = application.CreateClient()

        let typedClient = MyOpenapi.Client(client)

        task {

            let! forecast = typedClient.GetWeatherForecast()

            Assert.NotNull(forecast.[0].Date)
        }

    [<Fact>]
    let ``GET hello returns hello with stub and stubbery`` () =
        task {

            use stub = new Stubbery.ApiStub()
            stub.Get("/externalApi", fun r args -> """{ "ok" : "hello" }""" |> box) |> ignore
            stub.Post("/anotherApi", fun r args -> """{ "ok" : "hello" }""" |> box) |> ignore

            let uri : MutableUri = { MockUri = new Uri("http://test") }

            let application =
                (new WebApplicationFactory<Startup>())
                    .WithWebHostBuilder(fun b ->
                        b.ConfigureTestServices(fun s ->
                            s.ConfigureAll<HttpClientFactoryOptions>(fun options ->
                                options.HttpClientActions.Add(fun c -> 
                                    c.BaseAddress <- uri.MockUri)
                            ) |> ignore
                        ) |> ignore
                    )

            let client = application.CreateClient()
            stub.Start()
            uri.MockUri <- new Uri(stub.Address)

            let! hello = client.GetAsync("/Hello")

            Assert.NotNull(hello)

            }
            
    [<Fact>]
    let ``test stubbery with extension works`` () =

        task {

            let testApp =
                testce_stubbery () { 
                    GET "/externalApi" (fun r args -> {| Ok = "yeah" |} |> box)
                    POST "/anotherApi" (fun r args -> {| Ok = "yeah" |} |> box)
                }

            use client = testApp.GetFactory().CreateClient()

            let! r = client.GetAsync("/Hello")

            let! rr =
                r.EnsureSuccessStatusCode()
                    .Content.ReadAsStringAsync()

            Assert.NotEmpty(rr)
        } 

    [<Fact>]
    let ``test stubbery with swagger gen client for json apis`` () =

        task {
            let expected =  {| Ok = "yeah" |}

            let testApp =
                testce_stubbery () { 
                    GET "/externalApi" (fun r args -> expected |> box)
                    POST "/anotherApi" (fun r args -> expected |> box)
                }

            use client = testApp.GetFactory().CreateClient()
            let typedClient = new MyOpenapi.Client(client)

            let! r = typedClient.GetHello()

            Assert.Equal(expected.Ok, r.Ok)
        } 

    [<Fact>]
    let ``test with extension works no stubbery`` () =

        task {

            let expected = {| Ok = "yeah" |}

            let testApp =
                testce () { 
                    GETJ "/externalApi" expected
                    POSTJ "/another/anotherApi" {| Test = "hello" ; Time = 1|}
                    POST "/notUsed" (fun _ _ -> "ok" |> R_TEXT)
                    POST "/notUsed2" (fun _ _ -> "ok" |> R_TEXT)
                    POST "/errRoute" (fun _ _ -> R_ERROR HttpStatusCode.NotAcceptable (new StringContent("err")))
                }

            use client = testApp.GetFactory().CreateClient()

            let! r = client.GetAsync("/Hello")

            r.EnsureSuccessStatusCode() |> ignore

            let! rr = r.Content.ReadFromJsonAsync<MyOpenapi.Hello>()

            Assert.Equal(expected.Ok, rr.Ok)
        } 

    [<Fact>]
    let ``test with extension works two clients`` () =

        task {

            let expected = {| Ok = "yeah" |}

            let testApp =
                testce () { 
                    GETJ "/externalApi" expected
                    POST "/another/anotherApi" (fun _ _ -> {| Test = "hello" ; Time = 1|} |> R_JSON)
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
    let ``test with swagger gen client for json apis`` () =

        task {
            let expected =  {| Ok = "yeah" |}

            use testApp =
                testce () { 
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
    let ``check custom client override still allowed before`` () =

        task {
            let expected =  {| Ok = "yeah" |}

            use testApp =
                testce () { 
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
    let ``check custom client override still allowed after`` () =

        task {

            let expected =  {| Ok = "yeah" |}

            use testApp =
                testce () { 
                    GETJ "externalApi" expected
                    POSTJ "test/anotherApi" expected  
                }

            let factory = 
                testApp.GetFactory()
                |> web_configure_test_services (fun t -> 
                    t.AddHttpClient("anotherApiClient", configureClient =
                        (fun c -> c.BaseAddress <- new Uri("http://localhost/test/"))
                    )
                )
                
            let clientFactory = factory.Services.GetRequiredService<IHttpClientFactory>()
            let customClient = clientFactory.CreateClient("anotherApiClient")

            Assert.Equal("http://localhost/test/", customClient.BaseAddress.ToString())

            let client = factory.CreateClient()
            let typedClient = new MyOpenapi.Client(client)

            let! r = typedClient.GetHello()

            test <@ expected.Ok = r.Ok @>
        }