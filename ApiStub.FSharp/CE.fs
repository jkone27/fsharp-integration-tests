namespace ApiStub.FSharp

open Microsoft.AspNetCore.Mvc.Testing
open Microsoft.Extensions.DependencyInjection
open System.Threading.Tasks
open System.Net.Http
open System
open Microsoft.Extensions.Http
open Microsoft.AspNetCore.Routing.Template
open Microsoft.AspNetCore.Routing
open System.Net

module CE =
    open BuilderExtensions
    open HttpResponseHelpers
    open DelegatingHandlers

    /// NOT IN USE, check if needed
    type ResponseStreamWrapperHandler(handler:  HttpMessageHandler) =
        inherit DelegatingHandler(handler)

        override this.SendAsync(request, cancellationToken) =
            
            let wrappedBase = new AsyncCallableHandler(base.InnerHandler) 
            
            task {
                let! response = wrappedBase.CallSendAsync(request, cancellationToken);

                let! bytes = response.Content.ReadAsByteArrayAsync(cancellationToken)

                let newContent = new ByteArrayContent(bytes)

                for sourceHeader in response.Content.Headers do
                    let vals = sourceHeader.Value |> Seq.toArray
                    newContent.Headers.Add(sourceHeader.Key, vals)

                response.Content <- newContent

                return response
            }

    type MockTerminalHandler() = 
        inherit HttpMessageHandler()

        override this.SendAsync(_, token) =
            task {
                token.ThrowIfCancellationRequested()
            
                return 
                    new StringContent("No Stubs Specified for This Call")
                    |> R_ERROR HttpStatusCode.BadRequest
            }         
    
    type TestClient<'T when 'T: not struct>() =

        let factory = new WebApplicationFactory<'T>()
        let mutable httpMessageHandler : DelegatingHandler = null
        let customConfigureServices = new ResizeArray<IServiceCollection -> obj>()

        interface IDisposable 
                with member this.Dispose() =
                        factory.Dispose()

        interface IAsyncDisposable
                with member this.DisposeAsync() =
                        factory.DisposeAsync()

        member this.Yield(()) = (factory, httpMessageHandler, customConfigureServices)

        /// generic stub operation with stub function
        [<CustomOperation("stub_with_options")>]
        member this.StubWithOptions(_, methods, routeTemplate : string, stub: HttpRequestMessage -> RouteValueDictionary -> HttpResponseMessage, useRealHttpClient) =
            
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
                let baseClient : HttpMessageHandler = 
                    if useRealHttpClient then 
                        new HttpClientHandler()
                    else
                        new ResponseStreamWrapperHandler(new MockTerminalHandler())
                httpMessageHandler <- new MockClientHandler(baseClient, methods, templateMatcher.Value, stub)
            else
                httpMessageHandler <- new MockClientHandler(httpMessageHandler, methods, templateMatcher.Value, stub)

            this

        [<CustomOperation("stub")>]
        member this.Stub(x, methods, routeTemplate, stub: HttpRequestMessage -> RouteValueDictionary -> HttpResponseMessage)=
            this.StubWithOptions(x, methods, routeTemplate, stub, false)

        /// stub operation with stub object (HttpResponseMessage)
        [<CustomOperation("stub_obj")>]
        member this.StubObj(x, methods, routeTemplate, stub: unit -> HttpResponseMessage) =
            this.Stub(x, methods, routeTemplate, fun _ _ -> stub())

        /// string stub
        [<CustomOperation("stubs")>]
        member this.StubString(x, methods, routeTemplate, stub: string) =
            this.StubObj(x, methods, routeTemplate, fun _ -> stub |> R_TEXT)

        /// json stub
        [<CustomOperation("stubj")>]
        member this.StubJson(x, methods, routeTemplate, stub: obj) =
            this.StubObj(x, methods, routeTemplate, fun _ -> stub |> R_JSON)

        /// stub GET request with stub function
        [<CustomOperation("GET")>]
        member this.Get(x, route, stub) =
            this.Stub(x, [|HttpMethod.Get|], route, stub)

        /// stub GET request with stub object
        [<CustomOperation("GET")>]
        member this.Get2(x, route, stub) =
            this.StubObj(x, [|HttpMethod.Get|], route, stub)

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
                        //builder.AdditionalHandlers.Add(httpMessageHandler) |> ignore
                        builder.PrimaryHandler <- httpMessageHandler
                    )

                    options.HttpClientActions.Add(fun c -> 
                        let path =
                            if c.BaseAddress <> null then
                                c.BaseAddress.AbsolutePath
                            else
                                String.Empty

                        let newBase = new Uri(new Uri("http://127.0.0.1/"), path)
                        c.BaseAddress <- newBase
                    ) |> ignore
                ) |> ignore

                for custom_config in customConfigureServices do
                    custom_config(s) 
                    |> ignore

            )

                    
