namespace ApiStub.FSharp

open System.Threading.Tasks
open System.Net.Http
open System
open Microsoft.AspNetCore.Routing.Template
open Microsoft.AspNetCore.Routing
open Microsoft.AspNetCore.Http

module DelegatingHandlers =

    type internal AsyncCallableHandler(messageHandler) =
        inherit DelegatingHandler(messageHandler)
        member internal x.CallSendAsync(request, cancellationToken) =
            base.SendAsync(request, cancellationToken)
    
    type MockClientHandler(handler : HttpMessageHandler, methods, templateMatcher: TemplateMatcher, responseStubber) = 
        inherit DelegatingHandler(handler)

        override this.SendAsync(request, token) =
            let wrappedBase = new AsyncCallableHandler(base.InnerHandler) 
            task {
                let routeDict = new RouteValueDictionary()
                if methods |> Array.contains(request.Method) |> not then
                    return! wrappedBase.CallSendAsync(request, token)
                else if templateMatcher.TryMatch(request.RequestUri.AbsolutePath |> PathString, routeDict) |> not then
                    return! wrappedBase.CallSendAsync(request, token)
                else
                    // HTTP response stubbing happens here, the request has matched, go on with the stub
                    let mutable expected : HttpResponseMessage = responseStubber request routeDict
                    // reattach original request!!!
                    expected.RequestMessage <- request
                    return expected
            }
        
