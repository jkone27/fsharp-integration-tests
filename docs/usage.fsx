(*** hide ***)
#r "nuget: FSharp.Formatting, 11.3.0"

(**
## Usage

To use the CE, you must build your CE object first by passing the generic `Program` (minimal api) or `Startup` (mvc) type argument to `TestClient<T>`.

### Sample Use Case

Suppose in your main app (`Program` or `Startup`) you call `Services.AddHttpClient`(or its variants) twice, registering 2 API clients to make calls to other services, say to the outbound routes `/externalApi` and `/anotherApi` (let's skip the base address for now).
suppose `ExternalApiClient` invokes an http `GET` method and the other client makes a `POST` http call, inside your API client code. 

<br>
<div class="mermaid text-center">
sequenceDiagram
    Test->>App: GET /Hello
    App->>ApiDep1: GET /externalApi
    ApiDep1-->>App: Response
    App->>ApiDep2: POST /anotherApi
    ApiDep2-->>App: Response
    App-->>Test: Response
</div>
<br>
### HTTP Mocks 🤡

It's easy to **mock** those http clients dependencies (with data stubs) during integration tests making use of `ApiStub.FSharp` lib, saving quite some code compared to manually implementing the `WebApplicationFactory<T>` pattern, let's see how below.

## F#

* `Program`: to be able to make use of `Program.fs` (e.g. minimal api) as `TestClient<Program>()`, make sure to declare an empty `type Program = end class` on top of your Program module containing the `[<EntryPoint>] main args` method.


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

## C# - v.1.1.0

if you prefer to use C# for testing, some extension methods are provided to use with C# as well:  

`GETJ, PUTJ, POSTJ, DELETEJ`

If you want to access more overloads, you can access the inspect `TestClient<T>` members and create your custom extension methods easilly.

```csharp
using ApiStub.FSharp;
using static ApiStub.Fsharp.CsharpExtensions; 

var webAppFactory = new CE.TestClient<Web.Sample.Program>()
    .GETJ(Clients.Routes.name, new { Name = "Peter" })
    .GETJ(Clients.Routes.age, new { Age = 100 })
    .GetFactory();

// factory.CreateClient(); // as needed later in your tests

```
