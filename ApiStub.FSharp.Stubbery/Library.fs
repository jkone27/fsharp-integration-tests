namespace ApiStub.FSharp.Stubbery

open Microsoft.AspNetCore.Mvc.Testing
open Microsoft.Extensions.DependencyInjection
open System.Net.Http
open System
open Microsoft.Extensions.Http
open ApiStub.FSharp

module StubberyCE =

    open BuilderExtensions

    type MutableUri = { mutable MockUri: Uri }

    type TestStubberyClient<'T when 'T: not struct>() =

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
                .Response(fun r args -> stub r args |> box)
            |> ignore

            this

        [<CustomOperation("stub_obj")>]
        member this.StubObj(x, methods, route, stub: unit -> obj) =
            this.Stub(x, methods, route, (fun _ _ -> stub ()))

        [<CustomOperation("GET")>]
        member this.Get(x, route, stub) =
            this.Stub(x, [| HttpMethod.Get |], route, stub)

        [<CustomOperation("GET_OBJ")>]
        member this.GetObj(x, route, stub) =
            this.StubObj(x, [| HttpMethod.Get |], route, (fun _ -> stub))

        [<CustomOperation("POST")>]
        member this.Post(x, route, stub) =
            this.Stub(x, [| HttpMethod.Post |], route, stub)

        [<CustomOperation("POST_OBJ")>]
        member this.PostObj(x, route, stub) =
            this.StubObj(x, [| HttpMethod.Post |], route, (fun _ -> stub))

        [<CustomOperation("PUT")>]
        member this.Put(x, route, stub) =
            this.Stub(x, [| HttpMethod.Put |], route, stub)

        [<CustomOperation("PUT_OBJ")>]
        member this.PutObj(x, route, stub) =
            this.StubObj(x, [| HttpMethod.Put |], route, (fun _ -> stub))

        [<CustomOperation("PATCH")>]
        member this.Patch(x, route, stub) =
            this.Stub(x, [| HttpMethod.Patch |], route, stub)

        [<CustomOperation("PATCH_OBJ")>]
        member this.PatchObj(x, route, stub) =
            this.StubObj(x, [| HttpMethod.Patch |], route, (fun _ -> stub))


        [<CustomOperation("DELETE")>]
        member this.Delete(x, route, stub) =
            this.Stub(x, [| HttpMethod.Delete |], route, stub)

        member this.GetFactory() =
            let clientBuilder =
                factory
                |> web_configure_services (fun s ->
                    s.ConfigureAll<HttpClientFactoryOptions>(fun options ->
                        options.HttpClientActions.Add(fun c -> c.BaseAddress <- uri.MockUri)))

            stubbery.Start()
            uri.MockUri <- new Uri(stubbery.Address)
            clientBuilder

        interface IDisposable with
            member this.Dispose() =
                factory.Dispose()
                stubbery.Dispose()
