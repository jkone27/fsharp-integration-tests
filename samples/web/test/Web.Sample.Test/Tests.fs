module Tests

open System
open Xunit
open ApiStub.FSharp
open System.Threading.Tasks
open ApiStub.FSharp.CE
open Web.Sample
open System.Net.Http.Json


let webAppFactory = 
    new TestWebAppFactoryBuilder<Web.Sample.Program>() 
    |> fun x -> x {
        GETJ Web.Sample.Clients.Routes.name {| Name = "Peter" |}
        GETJ Web.Sample.Clients.Routes.age {| Age = 100 |}
    }
    |> _.GetFactory()

[<Fact>]
let ``Peter is 100 years old`` () =
    task {
        let c = webAppFactory.CreateClient()

        let! res = c.PostAsJsonAsync(Web.Sample.Services.routeOne, {| |})
        let! responseText = res.Content.ReadAsStringAsync()

        Assert.True(res.IsSuccessStatusCode)
        Assert.Contains("Peter", responseText)
        Assert.Contains("100", responseText)
    }
