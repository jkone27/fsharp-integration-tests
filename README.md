# ApiStub.FSharp

You have an ASP NET NET5/6 dotnet api, and you want to simplify http stubs for integration
testing, so you can make use of this Computation Expressions (CE) to simplify
your tests with some integration testing http stubs DSL. 

A version using the Stubbery library is also present for "compatibility" when migrating from stubbery versions in your integration tests setup.

Important, to use the CE, you have to build your CE object first by passing the generic Startup type argument. Because of how it's implemented still needs you to provide a Startup class, future version might make use of Program only from minimal api (this already uses WebApplication only anyway).

## Usage

```fsharp
open ApiStub.FSharp.CE
open ApiStub.FSharp.BuilderExtensions
open ApiStub.FSharp.HttpResponseHelpers
open Xunit

module Tests =

    // build your aspnetcore integration testing CE
    let test () = new TestClient<Startup>()

    [<Fact>]
    let ``Integration test calls Hello and returns Success`` () =

        task {

            let testApp =
                test () { 
                    GETJ "/externalApi" {| Ok = "yeah" |}
                    POSTJ "/anotherApi" {| Whatever = "yeah" |}
                    GETJ "/yetAnotherOne" {| Success = true |}
                }

            use client = testApp.GetFactory().CreateClient()

            let! r = client.GetAsync("/Hello")

            r.EnsureSuccessStatusCode()
        } 
```


## ApiStub.FSharp.Stubbery

```fsharp
open ApiStub.FSharp.Stubbery.StubberyCE
open ApiStub.FSharp.BuilderExtensions
open ApiStub.FSharp.HttpResponseHelpers
open Xunit

module Tests =

    // build your aspnetcore integration testing CE using Stubbery library
    // for serving HTTP stubs
    let test_stubbery () = new TestStubberyClient<Startup>()

    [<Fact>]
    let ``Integration test with stubbery`` () =

        task {

            let testApp =
                test_stubbery () { 
                    GET "/externalApi" (fun r args -> expected |> box)
                }

            use client = testApp.GetFactory().CreateClient()

            let! r = client.GetAsync("/Hello")

            r.EnsureSuccessStatusCode()
        } 
```