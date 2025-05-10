namespace Web.Sample

#nowarn 20 // to avoid |> ignore everywhere in aspnet config files

open System
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open System.Threading.Tasks
open Microsoft.AspNetCore.Http

module Clients = 
    open System.Net.Http
    open System.Net.Http.Json

    module Routes = 
        let name = "/hello/name"
        let age = "/hello/age"

    let ``localhost:5000`` (httpClient: HttpClient) = 
        httpClient.BaseAddress <- "http://localhost" |> Uri

    type ClientOne(httpClient: HttpClient) =  
        member this.GetNameAsync() =
            httpClient.GetFromJsonAsync<{| Name: string |}>(Routes.name)

    type ClientTwo(httpClient: HttpClient) =  
        member this.GetAgeAsync() =
            httpClient.GetFromJsonAsync<{| Age: int |}>(Routes.age)


module Services = 
    open Clients

    let routeOne = "/service-one"

    type ServiceOne(clientOne: ClientOne, clientTwo: ClientTwo) =
        member this.GetAndPrintAsync() =
            task {
                let! name = clientOne.GetNameAsync()
                let! age = clientTwo.GetAgeAsync()

                return $"name: {name}, age:{age}"
            }

// IMPORTANT: needed for WebApplicationFactory<T>
type Program() = class end

// entry point is allowed only in let function bindings, so we need to also have this
module Program =

    [<EntryPoint>]
    let main args =

        let builder = 
            WebApplication.CreateBuilder(args)
            |> fun x -> 
                x.Services.AddHttpClient<Clients.ClientOne>(Clients.``localhost:5000``)
                x.Services.AddHttpClient<Clients.ClientTwo>(Clients.``localhost:5000``)
                x.Services.AddTransient<Services.ServiceOne>()
                x

        let app = builder.Build()

        // our app service is invoked in this route
        app.MapPost(Services.routeOne, Func<HttpContext, _>(fun c -> 
            task {
                let s = c.RequestServices.GetRequiredService<Services.ServiceOne>()

                let! r = s.GetAndPrintAsync()

                return r
            })
        )

        // test api client against these endpoints to avoid extra server hosting in app / containers etc
        app.MapGet("/hello/name", Func<{| Name: string |}>(fun () -> {| Name = "john" |}))
        app.MapGet("/hello/age", Func<{| Age: int |}>(fun () -> {| Age = 25 |}))

        app.Run()

        0 // Exit code

