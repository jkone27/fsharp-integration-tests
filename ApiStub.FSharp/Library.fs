namespace ApiStub.FSharp

open Microsoft.AspNetCore.Mvc.Testing
open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.Hosting
open System.Threading.Tasks
open System.Net.Http
open System
open Microsoft.AspNetCore.TestHost
open Microsoft.Extensions.Http
open Microsoft.AspNetCore.Routing.Template
open Microsoft.AspNetCore.Routing
open System.Net
open Microsoft.AspNetCore.Http
open System.Net.Http.Json

module BuilderExtensions =

    let configure_services (configure : IServiceCollection -> 'a) (builder: IWebHostBuilder) : IWebHostBuilder =
        builder.ConfigureServices(fun s -> configure(s) |> ignore)

    let configure_test_services (configure : IServiceCollection -> 'a) (builder: IWebHostBuilder) : IWebHostBuilder =
        builder.ConfigureTestServices(fun s -> configure(s) |> ignore)

    let web_host_builder (builder : IWebHostBuilder -> 'a) (factory: WebApplicationFactory<'T>)   =
        factory.WithWebHostBuilder(fun b -> builder(b) |> ignore)

    let web_configure_services configure =
        configure_services configure 
        |> web_host_builder

    let web_configure_test_services configure =
        configure_test_services configure
        |> web_host_builder

module HttpResponseHelpers =

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

module DelegatingHandlers =
    
    type MockClientHandler(methods, templateMatcher: TemplateMatcher, responseStubber) as this = 
        inherit DelegatingHandler()

        override this.SendAsync(request, token) =
            let routeDict = new RouteValueDictionary()
            if methods |> Array.contains(request.Method) |> not then
                base.SendAsync(request, token)
            else if templateMatcher.TryMatch(request.RequestUri.PathAndQuery |> PathString, routeDict) |> not then
                base.SendAsync(request, token)
            else
                responseStubber request routeDict
                |> Task.FromResult
       

module CE =
    open BuilderExtensions
    open HttpResponseHelpers
    open DelegatingHandlers
    
    type TestClient<'T when 'T: not struct>() as this =

        let factory = new WebApplicationFactory<'T>()
        let delegatingHandlers = new ResizeArray<DelegatingHandler>()
        let customConfigureServices = new ResizeArray<IServiceCollection -> obj>()

        member this.Yield(()) = (factory, delegatingHandlers, customConfigureServices)

        /// generic stub operation with stub function
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

        /// stub operation with stub object (HttpResponseMessage)
        [<CustomOperation("stub")>]
        member this.Stub2(x, methods, routeTemplate, stub: HttpResponseMessage) =
            this.Stub(x, methods, routeTemplate, fun _ _ -> stub)

        /// string stub
        [<CustomOperation("stubs")>]
        member this.StubString(x, methods, routeTemplate, stub: string) =
            this.Stub2(x, methods, routeTemplate, stub |> R_OK)

        /// json stub
        [<CustomOperation("stubj")>]
        member this.StubJson(x, methods, routeTemplate, stub) =
            this.Stub2(x, methods, routeTemplate, stub |> R_JSON)

        /// stub GET request with stub function
        [<CustomOperation("GET")>]
        member this.Get(x, route, stub) =
            this.Stub(x, [|HttpMethod.Get|], route, stub)

        /// stub GET request with stub object
        [<CustomOperation("GET")>]
        member this.Get2(x, route, stub) =
            this.Stub2(x, [|HttpMethod.Get|], route, stub)

        /// stub GET json
        [<CustomOperation("GETJ")>]
        member this.GetJson(x, route, stub) =
            this.StubJson(x, [|HttpMethod.Get|], route, stub)

        /// stub POST
        [<CustomOperation("POST")>]
        member this.Post(x, route, stub) =
            this.Stub(x, [|HttpMethod.Post|], route, stub)

        /// stub POST json
        [<CustomOperation("POSTJ")>]
        member this.PostJson(x, route, stub) =
            this.StubJson(x, [|HttpMethod.Post|], route, stub)

        /// stub PUT
        [<CustomOperation("PUT")>]
        member this.Put(x, route, stub) =
            this.Stub(x, [|HttpMethod.Put|], route, stub)

        /// stub PUT json
        [<CustomOperation("PUTJ")>]
        member this.PutJson(x, route, stub) =
            this.StubJson(x, [|HttpMethod.Put|], route, stub)

        /// stub DELETE
        [<CustomOperation("DELETE")>]
        member this.Delete(x, route, stub) =
            this.Stub(x, [|HttpMethod.Delete|], route, stub)

        /// stub DELETE json
        [<CustomOperation("DELETEJ")>]
        member this.DeleteJson(x, route, stub) =
            this.StubJson(x, [|HttpMethod.Delete|], route, stub)

        [<CustomOperation("config_services")>]
        member this.CustomConfigServices(x, customAction) =
            customConfigureServices.Add(customAction)

        member this.GetFactory() =
            factory
            |> web_configure_services (fun s ->
                s.ConfigureAll<HttpClientFactoryOptions>(fun options ->
                    options.HttpMessageHandlerBuilderActions.Add(fun builder ->
                        for dh in delegatingHandlers do
                            builder.AdditionalHandlers.Add(dh)
                    )
                ) |> ignore

                for custom_config in customConfigureServices do
                    custom_config(s) 
                    |> ignore

            )

        interface IDisposable 
                with member this.Dispose() =
                        factory.Dispose()
