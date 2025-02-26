(*** hide ***)
#r "nuget: FSharp.Formatting, 11.3.0"

(**
## Features 👨🏻‍🔬

* **HTTP client mock DSL**:
    * supports main HTTP verbs
    * support for JSON payload for automatic object serialization
* **BDD spec dsl extension** (behaviour driven development)
    * to express tests in gherkin GIVEN, WHEN, THEN format if you want to
* EXTRAS
    * utilities for test setup and more...

### Example

Here is an example of how to use the features of this library:

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
