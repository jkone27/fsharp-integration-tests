(*** hide ***)
#r "nuget: FSharp.Formatting, 11.3.0"

(**
## ApiStub.FSharp.Stubbery ⚠️ 🐦

A version using the [Stubbery](https://github.com/markvincze/Stubbery) library is also present for "compatibility" when migrating from `stubbery` versions of pre existing integration tests, in your integration tests setup.

In general, it's advised to not have dependencies or run any in-memory HTTP server if possible, so the minimal version is preferred.

NOTICE ⚠️: Stubbery will not be supported in adding new features or eventually might be not supported at all in the future.

### Example

Here is an example of how to use the Stubbery library with this library:

```fsharp
open ApiStub.FSharp.Stubbery.StubberyCE
open ApiStub.FSharp.BuilderExtensions
open ApiStub.FSharp.HttpResponseHelpers
open Xunit

module Tests =

    // build your aspnetcore integration testing CE using Stubbery library
    // for serving HTTP stubs
    let test_stubbery = new TestStubberyClient<Startup>()

    [<Fact>]
    let ``Integration test with stubbery`` () =

        task {

            let testApp =
                test_stubbery { 
                    GET "/externalApi" (fun r args -> expected |> box)
                }

            use client = testApp.GetFactory().CreateClient()

            let! r = client.GetAsync("/Hello")

            r.EnsureSuccessStatusCode()
        } 
```
