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

type MyOpenapi = OpenApiClientProvider<"swagger.json">

module CE =

    type MutableUri = { mutable MockUri: Uri }

    type TestFactory<'T when 'T: not struct>() as this =
        inherit WebApplicationFactory<'T>()

        /// bool return parameter : continueBase
        member val WithHttpClient: HttpClient -> bool = fun _ -> false with get, set

        member val WithBuilder: IWebHostBuilder -> bool = fun _ -> false with get, set

        override this.ConfigureClient(httpClient) =
            if this.WithHttpClient(httpClient) then
                ``base``.ConfigureClient(httpClient)

        override this.ConfigureWebHost(builder) =
            if this.WithBuilder(builder) then
                ``base``.ConfigureWebHost(builder)

    type TestClient<'T when 'T: not struct>() as this =

        let factory = new TestFactory<'T>()
        let stubbery = new Stubbery.ApiStub()
        let uri = { MockUri = null }

        member this.Yield(()) = (factory, stubbery)

        [<CustomOperation("builder")>]
        member this.Build(_, builder: IWebHostBuilder -> bool) =
            factory.WithBuilder <- builder
            this

        [<CustomOperation("test_client")>]
        member this.TestClient(_, builder: HttpClient -> bool) =
            factory.WithHttpClient <- builder
            this

        [<CustomOperation("stub_port")>]
        member this.StubPort(_, port: int) =
            stubbery.Port <- port
            this

        [<CustomOperation("stub")>]
        member this.Stub(_, stub: Stubbery.ApiStub -> Stubbery.ISetup) =
            stub (stubbery) |> ignore
            this

        [<CustomOperation("stub_request")>]
        member this.StubRequest(_, methods, route, stub) =
            stubbery
                .Request(methods)
                .IfRoute(route)
                .Response(fun r args -> stub r args) |> ignore
            this

        member this.CreateTestClient() =
            let clientBuilder = factory.WithWebHostBuilder(fun b -> 
                    b.ConfigureTestServices(fun s ->
                        s.ConfigureAll<HttpClientFactoryOptions>(fun options ->
                            options.HttpClientActions.Add(fun c -> 
                                c.BaseAddress <- uri.MockUri
                            ) |> ignore
                        ) |> ignore
                    ) |> ignore
                )

            stubbery.Start()
            uri.MockUri <- new Uri(stubbery.Address)
            clientBuilder.CreateClient()


        member val Services = factory.Services

        interface IDisposable 
                with member this.Dispose() =
                        factory.Dispose()
                        stubbery.Dispose()

    // CE builder
    let test = new TestClient<Startup>()

module Tests =

    open CE

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
    let ``GET hello returns hello with stub`` () =

        let stub = new Stubbery.ApiStub()
        stub.Get("/externalApi", fun r args -> """{ "ok" : "hello" }""" |> box) |> ignore

        let uri = { MockUri = new Uri("http://test") }

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
            
        try
            task {

                let client = application.CreateClient()
                stub.Start()
                uri.MockUri <- new Uri(stub.Address)

                let! hello = client.GetAsync("/Hello")

                Assert.NotNull(hello)

            } |> Async.AwaitTask |> Async.RunSynchronously
        finally
            stub.Dispose()

    [<Fact>]
    let ``test with extension works`` () =

        task {

            let app =
                test { 
                    stub_request [|HttpMethod.Get|] "/externalApi" (fun r args -> {| Ok = "yeah" |} |> box)
                }

            use client = app.CreateTestClient()

            //let typedClient = MyOpenapi.Client(client)

            let! r = client.GetAsync("/Hello")

            let! rr =
                r.EnsureSuccessStatusCode()
                    .Content.ReadAsStringAsync()

            Assert.NotEmpty(rr)
        } 
