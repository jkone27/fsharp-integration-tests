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

type ISomeSingleton =
    interface
    end

type SomeSingleton(name: string) =
    class
        interface ISomeSingleton
    end

type BuilderExtensionsTests(testOutput: ITestOutputHelper) =

    let testce = new CE.TestClient<Startup>()

    interface IDisposable with
        member this.Dispose() = (testce :> IDisposable).Dispose()

    [<Fact>]
    member this.``WITH_SERVICES registers correctly``() = 
        task {
        
            let testApp = testce {
                    GETJ "hello" {| ResponseCode = 1001 |}
                    WITH_SERVICES (fun (s: IServiceCollection) -> 
                        s.AddSingleton<ISomeSingleton>(new SomeSingleton("John")))
                }

            let fac = testApp.GetFactory()

            let singleton = fac.Services.GetRequiredService<ISomeSingleton>()

            Assert.NotNull(singleton)

            let client = fac.Services.GetRequiredService<HttpClient>()

            let! resp = client.GetFromJsonAsync<{| ResponseCode: int |}>("hello")

            Assert.Equal(1001, resp.ResponseCode)
        }


    [<Fact>]
    member this.``WITH_TEST_SERVICES registers correctly``() = 
        task {

            let singletonMock = { new ISomeSingleton }
        
            let testApp = testce {
                    GETJ "hello" {| ResponseCode = 1001 |}
                    WITH_SERVICES (fun (s: IServiceCollection) -> 
                        s.AddSingleton<ISomeSingleton>(new SomeSingleton("John")))
                    WITH_TEST_SERVICES (fun s -> 
                        s.AddSingleton(singletonMock))
                }

            let fac = testApp.GetFactory()

            let singleton = fac.Services.GetRequiredService<ISomeSingleton>()

            Assert.NotNull(singleton)
            Assert.Same(singletonMock, singleton)

            let client = fac.Services.GetRequiredService<HttpClient>()

            let! resp = client.GetFromJsonAsync<{| ResponseCode: int |}>("hello")

            Assert.Equal(1001, resp.ResponseCode)
        }

    [<Fact>]
    member this.``HTTP MOCKS can use task async functions OK``() = 
        task {
        
            let testApp = testce {
                    GET_ASYNC "hello" (fun r args -> 
                        task {
                            return {| ResponseCode = 1001 |} |> R_JSON
                        }   
                    )        
                }

            use fac = testApp.GetFactory()

            let client = fac.Services.GetRequiredService<HttpClient>()

            let! resp = client.GetFromJsonAsync<{| ResponseCode: int |}>("hello")

            Assert.Equal(1001, resp.ResponseCode)
        }
