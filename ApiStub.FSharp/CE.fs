namespace ApiStub.FSharp

open Microsoft.AspNetCore.Mvc.Testing
open Microsoft.Extensions.DependencyInjection
open System.Threading.Tasks
open System.Net.Http
open System
open Microsoft.Extensions.Http
open Microsoft.AspNetCore.Routing.Template
open Microsoft.AspNetCore.Routing

/// computation expression module (builder CE), contains `TestClient<T>` that wraps `WebApplicationFactory<T>`
module CE =
    open BuilderExtensions
    open HttpResponseHelpers
    open DelegatingHandlers

    let private toAsync stub =
        fun req args -> task { return stub req args }

    /// `TestClient<T>` wraps `WebApplicationFactory<T>` and exposes a builder CE with utility to define api client stubs and other features
    type TestClient<'T when 'T: not struct>() =

        let factory = new WebApplicationFactory<'T>()
        let mutable httpMessageHandler: DelegatingHandler = null

        let customConfigureServices =
            new ResizeArray<IServiceCollection -> IServiceCollection>()

        let customConfigureTestServices =
            new ResizeArray<IServiceCollection -> IServiceCollection>()


        interface IDisposable with
            member this.Dispose() = factory.Dispose()

        interface IAsyncDisposable with
            member this.DisposeAsync() = factory.DisposeAsync()

        member this.Yield(()) =
            (factory, httpMessageHandler, customConfigureServices)


        /// generic stub operation with stub function
        [<CustomOperation("stub_with_options")>]
        member this.StubWithOptions
            (
                _,
                methods,
                routeTemplate: string,
                stubAsync: HttpRequestMessage -> RouteValueDictionary -> HttpResponseMessage Task,
                useRealHttpClient
            ) =

            let routeValueDict = new RouteValueDictionary()

            let templateMatcher =
                try

                    let rt = TemplateParser.Parse(routeTemplate.TrimStart('/'))
                    let tm = new TemplateMatcher(rt, routeValueDict)
                    Some(tm)
                with _ ->
                    None

            if templateMatcher.IsNone then
                failwith $"stub: error parsing route template for {routeTemplate}"

            if httpMessageHandler = null then
                // add nested handler
                let baseClient: HttpMessageHandler =
                    if useRealHttpClient then
                        new HttpClientHandler()
                    else
                        new ResponseStreamWrapperHandler(new MockTerminalHandler())

                httpMessageHandler <- new MockClientHandler(baseClient, methods, templateMatcher.Value, stubAsync)
            else
                httpMessageHandler <-
                    new MockClientHandler(httpMessageHandler, methods, templateMatcher.Value, stubAsync)

            this


        [<CustomOperation("stub")>]
        member this.Stub
            (
                x,
                methods,
                routeTemplate,
                stub: HttpRequestMessage -> RouteValueDictionary -> HttpResponseMessage
            ) =
            this.StubWithOptions(x, methods, routeTemplate, stub |> toAsync, false)

        [<CustomOperation("stub_async")>]
        member this.StubAsync
            (
                x,
                methods,
                routeTemplate,
                stub: HttpRequestMessage -> RouteValueDictionary -> HttpResponseMessage Task
            ) =
            this.StubWithOptions(x, methods, routeTemplate, stub, false)

        /// stub operation with stub object (HttpResponseMessage)
        [<CustomOperation("stub_obj")>]
        member this.StubObj(x, methods, routeTemplate, stub: unit -> HttpResponseMessage) =
            this.Stub(x, methods, routeTemplate, (fun _ _ -> stub ()))

        /// string stub
        [<CustomOperation("stubs")>]
        member this.StubString(x, methods, routeTemplate, stub: string) =
            this.StubObj(x, methods, routeTemplate, (fun _ -> stub |> R_TEXT))

        /// json stub
        [<CustomOperation("stubj")>]
        member this.StubJson(x, methods, routeTemplate, stub: obj) =
            this.StubObj(x, methods, routeTemplate, (fun _ -> stub |> R_JSON))

        /// stub GET request with stub function
        [<CustomOperation("GET_ASYNC")>]
        member this.GetAsync(x, route, stub) =
            this.StubAsync(x, [| HttpMethod.Get |], route, stub)

        /// stub GET request with stub function
        [<CustomOperation("GET")>]
        member this.Get(x, route, stub) =
            this.Stub(x, [| HttpMethod.Get |], route, stub)

        /// stub GET request with stub object
        [<CustomOperation("GET")>]
        member this.Get2(x, route, stub) =
            this.StubObj(x, [| HttpMethod.Get |], route, stub)

        /// stub GET json
        [<CustomOperation("GETJ")>]
        member this.GetJson(x, route, stub: obj) =
            this.StubJson(x, [| HttpMethod.Get |], route, stub)

        /// stub POST
        [<CustomOperation("POST_ASYNC")>]
        member this.PostAsync(x, route, stub) =
            this.StubAsync(x, [| HttpMethod.Post |], route, stub)

        /// stub POST
        [<CustomOperation("POST")>]
        member this.Post(x, route, stub) =
            this.Stub(x, [| HttpMethod.Post |], route, stub)

        /// stub POST json
        [<CustomOperation("POSTJ")>]
        member this.PostJson(x, route, stub: obj) =
            this.StubJson(x, [| HttpMethod.Post |], route, stub)

        /// stub PUT
        [<CustomOperation("PUT_ASYNC")>]
        member this.PutAsync(x, route, stub) =
            this.StubAsync(x, [| HttpMethod.Put |], route, stub)

        /// stub PUT
        [<CustomOperation("PUT")>]
        member this.Put(x, route, stub) =
            this.Stub(x, [| HttpMethod.Put |], route, stub)

        /// stub PUT json
        [<CustomOperation("PUTJ")>]
        member this.PutJson(x, route, stub: obj) =
            this.StubJson(x, [| HttpMethod.Put |], route, stub)

        /// stub DELETE
        [<CustomOperation("DELETE")>]
        member this.Delete(x, route, stub) =
            this.Stub(x, [| HttpMethod.Delete |], route, stub)

        /// stub DELETE
        [<CustomOperation("DELETE_ASYNC")>]
        member this.DeleteAsync(x, route, stub) =
            this.StubAsync(x, [| HttpMethod.Delete |], route, stub)

        /// stub DELETE json
        [<CustomOperation("DELETEJ")>]
        member this.DeleteJson(x, route, stub: obj) =
            this.StubJson(x, [| HttpMethod.Delete |], route, stub)

        [<CustomOperation("WITH_SERVICES")>]
        member this.CustomConfigServices(x, customAction) =
            customConfigureServices.Add(customAction)
            this

        [<CustomOperation("WITH_TEST_SERVICES")>]
        member this.CustomConfigTestServices(x, customAction) =
            customConfigureTestServices.Add(customAction)
            this

        member this.GetFactory() =
            factory
            |> web_configure_services (fun s ->
                s.ConfigureAll<HttpClientFactoryOptions>(fun options ->
                    options.HttpMessageHandlerBuilderActions.Add(fun builder ->
                        //builder.AdditionalHandlers.Add(httpMessageHandler) |> ignore
                        builder.PrimaryHandler <- httpMessageHandler)

                    options.HttpClientActions.Add(fun c ->
                        let path =
                            if c.BaseAddress <> null then
                                c.BaseAddress.AbsolutePath
                            else
                                String.Empty

                        let newBase = new Uri(new Uri("http://127.0.0.1/"), path)
                        c.BaseAddress <- newBase)
                    |> ignore)
                |> ignore

                for custom_config in customConfigureServices do
                    custom_config (s) |> ignore

            )
            |> web_configure_test_services (fun s ->
                for custom_config in customConfigureTestServices do
                    custom_config (s) |> ignore)
