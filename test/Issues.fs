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

module IssuesTests =

    let testce = new CE.TestClient<Startup>()

    [<Fact>]
    let ``test_no_mocks_declared_throws_exception`` () =
        task {
            let factory = testce.GetFactory()
            let client = factory.CreateClient()

            let! response = client.GetAsync("/Hello")
            Assert.False(response.IsSuccessStatusCode)
            let! responseString = response.Content.ReadAsStringAsync()
            Assert.Contains("no mocks were provided", responseString)
        }
