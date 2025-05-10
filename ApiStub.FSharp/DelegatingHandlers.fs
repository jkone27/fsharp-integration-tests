namespace ApiStub.FSharp

open System.Threading.Tasks
open System.Net.Http
open System
open Microsoft.AspNetCore.Routing.Template
open Microsoft.AspNetCore.Routing
open Microsoft.AspNetCore.Http
open ApiStub.FSharp.HttpResponseHelpers
open System.Net

module DelegatingHandlers =

    /// necessary to wrap base calls in handlers
    type internal AsyncCallableHandler(messageHandler) =
        inherit DelegatingHandler(messageHandler)

        member internal x.CallSendAsync(request, cancellationToken) =
            base.SendAsync(request, cancellationToken)

    /// <summary>This is the main stub/mock handler</summary>
    type MockClientHandler(handler: HttpMessageHandler, methods, templateMatcher: TemplateMatcher, responseStubber) =
        inherit DelegatingHandler(handler)

        override this.SendAsync(request, token) =
            let wrappedBase = new AsyncCallableHandler(base.InnerHandler)

            task {
                let routeDict = new RouteValueDictionary()

                if methods |> Array.contains (request.Method) |> not then
                    return! wrappedBase.CallSendAsync(request, token)
                else if
                    templateMatcher.TryMatch(request.RequestUri.AbsolutePath |> PathString, routeDict)
                    |> not
                then
                    return! wrappedBase.CallSendAsync(request, token)
                else
                    // HTTP response stubbing happens here, the request has matched, go on with the stub
                    let! (expected: HttpResponseMessage) = responseStubber request routeDict
                    // reattach original request!!!
                    expected.RequestMessage <- request
                    return expected
            }

    /// <summary>This handler comes into play when no matches are happening, returning a BAD REQUEST 400 to the client</summary>
    type MockTerminalHandler() =
        inherit HttpMessageHandler()

        override this.SendAsync(_, token) =
            task {
                token.ThrowIfCancellationRequested()

                return
                    new StringContent("No Stubs Specified for This Call")
                    |> R_ERROR HttpStatusCode.BadRequest
            }


    /// NOT IN USE, check if needed
    type ResponseStreamWrapperHandler(handler: HttpMessageHandler) =
        inherit DelegatingHandler(handler)

        override this.SendAsync(request, cancellationToken) =

            let wrappedBase = new AsyncCallableHandler(base.InnerHandler)

            task {
                let! response = wrappedBase.CallSendAsync(request, cancellationToken)

                let! bytes = response.Content.ReadAsByteArrayAsync(cancellationToken)

                let newContent = new ByteArrayContent(bytes)

                for sourceHeader in response.Content.Headers do
                    let vals = sourceHeader.Value |> Seq.toArray
                    newContent.Headers.Add(sourceHeader.Key, vals)

                response.Content <- newContent

                return response
            }
