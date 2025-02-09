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

open ApiStub.FSharp

module StubberyTests =

    open CE
    open BuilderExtensions
    open HttpResponseHelpers

    let testce_stubbery () =
        new Stubbery.StubberyCE.TestStubberyClient<Startup>()

    [<Fact>]
    let ``GET hello returns hello with stub and stubbery`` () =
        task {

            use stub = new Stubbery.ApiStub()

            stub.Get("/externalApi", (fun r args -> """{ "ok" : "hello" }""" |> box))
            |> ignore

            stub.Post("/anotherApi", (fun r args -> """{ "ok" : "hello" }""" |> box))
            |> ignore

            let uri: MutableUri = { MockUri = new Uri("http://test") }

            let application =
                (new WebApplicationFactory<Startup>())
                    .WithWebHostBuilder(fun b ->
                        b.ConfigureTestServices(fun s ->
                            s.ConfigureAll<HttpClientFactoryOptions>(fun options ->
                                options.HttpClientActions.Add(fun c -> c.BaseAddress <- uri.MockUri))
                            |> ignore)
                        |> ignore)

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

            let! rr = r.EnsureSuccessStatusCode().Content.ReadAsStringAsync()

            Assert.NotEmpty(rr)
        }

    [<Fact>]
    let ``test stubbery with swagger gen client for json apis`` () =

        task {
            let expected = {| Ok = "yeah" |}

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
