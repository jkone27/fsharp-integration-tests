﻿namespace test

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

type MyOpenapi = OpenApiClientProvider<"swagger.json">

module CE =
    open System.Net.Http.Json

    type MutableUri = { mutable MockUri: Uri }

    type TestStubberyClient<'T when 'T: not struct>() as this =

        let factory = new WebApplicationFactory<'T>()
        let stubbery = new Stubbery.ApiStub()
        let uri = { MockUri = null }

        member this.Yield(()) = (factory, stubbery)

        [<CustomOperation("stub_port")>]
        member this.StubPort(_, port: int) =
            stubbery.Port <- port
            this

        [<CustomOperation("custom_stub")>]
        member this.CustomStub(_, stub: Stubbery.ApiStub -> Stubbery.ISetup) =
            stub (stubbery) |> ignore
            this

        [<CustomOperation("stub")>]
        member this.Stub(_, methods, route, stub) =
            stubbery
                .Request(methods)
                .IfRoute(route)
                .Response(fun r args -> stub r args) |> ignore
            this

        [<CustomOperation("GET")>]
        member this.Get(x, route, stub) =
            this.Stub(x, [|HttpMethod.Get|], route, stub)

        [<CustomOperation("POST")>]
        member this.Post(x, route, stub) =
            this.Stub(x, [|HttpMethod.Post|], route, stub)

        [<CustomOperation("PUT")>]
        member this.Put(x, route, stub) =
            this.Stub(x, [|HttpMethod.Put|], route, stub)

        [<CustomOperation("DELETE")>]
        member this.Delete(x, route, stub) =
            this.Stub(x, [|HttpMethod.Delete|], route, stub)

        member this.GetFactory() =
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
            clientBuilder

        interface IDisposable 
                with member this.Dispose() =
                        factory.Dispose()
                        stubbery.Dispose()


    type MockClientHandler(methods, templateMatcher: TemplateMatcher, responseStubber) as this = 
        inherit DelegatingHandler()

        override this.SendAsync(request, token) =
            let routeDict = new RouteValueDictionary()
            if methods |> Array.contains(request.Method) |> not then
                base.SendAsync(request, token)
            else if templateMatcher.TryMatch(request.RequestUri.PathAndQuery, routeDict) |> not then
                base.SendAsync(request, token)
            else
                responseStubber request routeDict
                |> Task.FromResult
                
    let inline R_OK (x: string) =
        let response = new HttpResponseMessage(HttpStatusCode.OK)
        response.Content <- new StringContent(x, Text.Encoding.UTF8, "application/json")
        response

    let inline R_JSON x =
        let response = new HttpResponseMessage(HttpStatusCode.OK)
        response.Content <- JsonContent.Create(x)
        response

    let inline R_ERROR statusCode content =
        let response = new HttpResponseMessage(statusCode)
        response.Content <- content
        response

    type TestClient<'T when 'T: not struct>() as this =

        let factory = new WebApplicationFactory<'T>()
        let delegatingHandlers = new ResizeArray<DelegatingHandler>()

        member this.Yield(()) = (factory, delegatingHandlers)

        [<CustomOperation("stub")>]
        member this.Stub(_, methods, routeTemplate, stub: HttpRequestMessage -> RouteValueDictionary -> HttpResponseMessage) =
            
            let routeValueDict = new RouteValueDictionary()
            let templateMatcher = 
                try
                    let rt = TemplateParser.Parse(routeTemplate)
                    let tm = new TemplateMatcher(rt, routeValueDict)
                    Some(tm)
                with _ ->
                    None

            if templateMatcher.IsNone then
                failwith $"stub: error parsing route template for {routeTemplate}"

            delegatingHandlers.Add(new MockClientHandler(methods, templateMatcher.Value, stub))
            this

        [<CustomOperation("stub")>]
        member this.Stub2(x, methods, routeTemplate, stub: HttpResponseMessage) =
            this.Stub(x, methods, routeTemplate, fun _ _ -> stub)

        [<CustomOperation("stubs")>]
        member this.StubString(x, methods, routeTemplate, stub: string) =
            this.Stub2(x, methods, routeTemplate, stub |> R_OK)

        [<CustomOperation("stubj")>]
        member this.StubJson(x, methods, routeTemplate, stub) =
            this.Stub2(x, methods, routeTemplate, stub |> R_JSON)

        [<CustomOperation("GET")>]
        member this.Get(x, route, stub) =
            this.Stub(x, [|HttpMethod.Get|], route, stub)

        [<CustomOperation("GETJ")>]
        member this.GetJson(x, route, stub) =
            this.StubJson(x, [|HttpMethod.Get|], route, stub)

        [<CustomOperation("POST")>]
        member this.Post(x, route, stub) =
            this.Stub(x, [|HttpMethod.Post|], route, stub)

        [<CustomOperation("POSTJ")>]
        member this.PostJson(x, route, stub) =
            this.StubJson(x, [|HttpMethod.Post|], route, stub)

        [<CustomOperation("PUT")>]
        member this.Put(x, route, stub) =
            this.Stub(x, [|HttpMethod.Put|], route, stub)

        [<CustomOperation("PUTJ")>]
        member this.PutJson(x, route, stub) =
            this.StubJson(x, [|HttpMethod.Put|], route, stub)

        [<CustomOperation("DELETE")>]
        member this.Delete(x, route, stub) =
            this.Stub(x, [|HttpMethod.Delete|], route, stub)

        member this.GetFactory() =
            factory.WithWebHostBuilder(fun b -> 
                    b.ConfigureTestServices(fun s ->

                        s.ConfigureAll<HttpClientFactoryOptions>(fun options ->
                            options.HttpMessageHandlerBuilderActions.Add(fun builder ->
                                for dh in delegatingHandlers do
                                    builder.AdditionalHandlers.Add(dh)
                            ) |> ignore
                        ) |> ignore
                    ) |> ignore
                )

        interface IDisposable 
                with member this.Dispose() =
                        factory.Dispose()


    // CE builder
    let test_stubbery () = new TestStubberyClient<Startup>()

    //CE Builder without stubbery
    let test () = new TestClient<Startup>()

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
    let ``GET hello returns hello with stub and stubbery`` () =
        task {

            use stub = new Stubbery.ApiStub()
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
                test_stubbery () { 
                    GET "/externalApi" (fun r args -> {| Ok = "yeah" |} |> box)
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
                test_stubbery () { 
                    GET "/externalApi" (fun r args -> expected |> box)
                }

            use client = testApp.GetFactory().CreateClient()
            let typedClient = new MyOpenapi.Client(client)

            let! r = typedClient.GetHello()

            Assert.Equal(expected.Ok, r.Ok)
        } 

    [<Fact>]
    let ``test with extension works no stubbery`` () =

        task {

            let testApp =
                test () { 
                    GETJ "/externalApi" {| Ok = "yeah" |}
                    POSTJ "/anotherStub" {| Test = "hello" ; Time = 1|}
                    POST "/notUsed" (fun _ _ -> "ok" |> R_OK)
                    POST "/notUsed2" (fun _ _ -> "ok" |> R_OK)
                    POST "/errRoute" (fun _ _ -> R_ERROR HttpStatusCode.NotAcceptable ("err" |> StringContent))
                }

            use client = testApp.GetFactory().CreateClient()

            let! r = client.GetAsync("/Hello")

            let! rr =
                r.EnsureSuccessStatusCode()
                    .Content.ReadAsStringAsync()

            Assert.NotEmpty(rr)
        } 

    [<Fact>]
    let ``test with swagger gen client for json apis`` () =

        task {
            let expected =  {| Ok = "yeah" |}

            let testApp =
                test () { 
                    GETJ "/notUsed" expected
                    GETJ "/externalApi" expected
                    POSTJ "/notUsed" expected
                    
                }

            use client = testApp.GetFactory().CreateClient()
            let typedClient = new MyOpenapi.Client(client)

            let! r = typedClient.GetHello()

            Assert.Equal(expected.Ok, r.Ok)
        } 

    [<Fact>]
    let ``check custom client override still allowed`` () =

        task {
            let expected =  {| Ok = "yeah" |}

            let testApp =
                test () { 
                    POSTJ "/another" expected
                }

            //let privateMock = new MockClientHandler()

            let factory = testApp.GetFactory().WithWebHostBuilder(fun (b : IWebHostBuilder) -> 
                b.ConfigureTestServices(fun (t: IServiceCollection) -> 
                        t.AddHttpClient("customClient", configureClient =
                            (fun c -> c.BaseAddress <- new Uri("http://localhost/else"))
                        ) |> ignore
                    ) |> ignore
                )
            let clientFactory = factory.Services.GetRequiredService<IHttpClientFactory>()
            let customClient = clientFactory.CreateClient("customClient")

            Assert.Equal("http://localhost/else", customClient.BaseAddress.ToString())
        }