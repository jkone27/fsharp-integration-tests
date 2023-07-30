namespace ApiStub.FSharp.Tests

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
open Microsoft.AspNetCore.Http
open System.Net.Http.Json
open Swensen.Unquote
open ApiStub.FSharp.CE
open ApiStub.FSharp.BuilderExtensions
open ApiStub.FSharp.HttpResponseHelpers
open ApiStub.FSharp
open ApiStub.FSharp.BDD
open HttpResponseMessageExtensions
open Xunit.Abstractions


module BDDTests =

    let testce = new TestClient<Startup>()


    [<Fact>]
    let ``when i call /hello i get 'world' back with 200 ok`` () =
            
            let mutable expected = "_"
            let stubData = { Ok = "undefined" }
                
            testce {
                POSTJ "/another/anotherApi" {| Test = "NOT_USED_VAL" |}
                GET_ASYNC "/externalApi" (fun r _ -> task { 
                    return { stubData with Ok = expected } |> R_JSON 
                })
            }
            |> SCENARIO "when i call /Hello i get 'world' back with 200 ok"
            |> SETUP (fun s -> task {
            
                let test = s.TestClient
                
                let f = test.GetFactory() 
                
                return {
                    Client = f.CreateClient()
                    Factory = f
                    Scenario = s
                    FeatureStubData = stubData
                }
            }) (fun c -> c)
            |> GIVEN (fun g -> 
                expected <- "world"
                expected |> Task.FromResult
            )
            |> WHEN (fun g -> task {
                let! (r : HttpResponseMessage) = g.Environment.Client.GetAsync("/Hello")
                return! r.Content.ReadFromJsonAsync<Hello>()

            })
            |> THEN (fun w -> 
                Assert.Equal(w.Given.ArrangeData, w.AssertData.Ok) 
            )
            |> END

