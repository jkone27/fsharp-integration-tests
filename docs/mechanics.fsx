(*** hide ***)
#r "nuget: FSharp.Formatting, 11.3.0"

(**
## Mechanics

This library makes use of [F# computation expressions](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/computation-expressions) to hide some complexity of `WebApplicationFactory` and provide the user with a *domain specific language* (DSL) for integration tests in aspnetcore apps.
The best way to understand how it all works is checking the [code](https://github.com/jkone27/fsharp-integration-tests/blob/249c3244cd7e20e2168b82a49b6e7e14f2ad1004/ApiStub.FSharp/CE.fs#L176) and this member CE method `GetFactory()` in scope.

If you have ideas for improvements feel free to open an issue/discussion! 
I do this on my own time, so support is limited but contributions/PRs are welcome 🙏 

### Example

Here is an example of how to use F# computation expressions with this library:

```fsharp
open ApiStub.FSharp.CE
open ApiStub.FSharp.BuilderExtensions
open ApiStub.FSharp.HttpResponseHelpers
open Xunit

module Tests =

    // build your aspnetcore integration testing CE
    let test = new TestClient<Startup>()

    [<Fact>]
    let ``Calls Hello and returns OK`` () =

        task {

            let testApp =
                test { 
                    GETJ "/externalApi" {| Ok = "yeah" |}
                    POSTJ "/anotherApi" {| Whatever = "yeah" |}
                }

            use client = testApp.GetFactory().CreateClient()

            let! r = client.GetAsync("/Hello")

            r.EnsureSuccessStatusCode()
        } 
```
