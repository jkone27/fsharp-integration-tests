// #i """nuget:C:\Repositories\fsharp-integration-tests\ApiStub.FSharp\bin\Debug"""
#r "nuget: ApiStub.FSharp, 1.0.0"
#r "nuget: Microsoft.Extensions.Hosting"
#r "nuget: Microsoft.Extensions.DependencyInjection"
#r "nuget: Microsoft.Extensions.DependencyInjection.Abstractions"

open ApiStub.FSharp
open Microsoft.AspNetCore
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection.Abstractions
open CE
open BuilderExtensions
open HttpResponseHelpers
open System.Threading.Tasks

type Startup() =
    member this.ConfigureServices(services) = ()

    member this.Configure(app) = ()


module Test =

    // build your aspnetcore integration testing CE
    let test () = new CE.TestClient<Startup>()

    let ``test`` () =
        task {

            let testApp = test () { GETJ "/externalApi" {| Ok = "yeah" |} }

            use client = testApp.GetFactory().CreateClient()

            let! r = client.GetAsync("/Hello")

            r.EnsureSuccessStatusCode()
        }
