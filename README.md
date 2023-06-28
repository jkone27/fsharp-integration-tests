# ApiStub.FSharp

You have an ASP NET NET5/6 dotnet api, and you want to simplify http stubs for integration
testing, so you can make use of this Computation Expressions (CE) to simplify
your tests with some integration testing http stubs DSL. 

Important, to use the CE, you have to build your CE object first by passing the generic Startup type argument. Because of how it's implemented still needs you to provide a Startup class, future version might make use of Program only from minimal api (this already uses WebApplication only anyway).

## USAGE

Suppose your server registers 2 api clients internally to make calls to other services, say to the outbound routes `externalApi` and `anotherApi`,
one client using `GET` and another using `POST` methods inside your api client code. 

It's easy to **mock** those endpoints (with data stubs) during integration tests in this way (or similar)...

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

## HTTP MOCKING METHODS

Available HTTP mocking methods in the test dsl are: 

* `GET`, `PUT`, `POST`, `DELETE` - for accessing request, route parameters and sending back HttpResponseMessage (e.g. using R_JSON or other constructors)

```fsharp
    // example of control on request and route value dictionary
    PUT "/externalApi" (fun r rvd -> 
        // read request properties or route, but not content...
        // unless you are willing to wait the task explicitly as result
        {| Success = true |} |> R_JSON 
    )
```

* `GETJ`, `PUTJ`, `POSTJ`, `DELETEJ` - for objects converted to json content

```fsharp
GETJ "/yetAnotherOne" {| Success = true |}
```

* `GET_ASYNC`, `PUT_ASYNC`, `POST_ASYNC`, `DELETE_ASYNC` - for handling asynchronous requests inside a task computation expression (async/await) and mock dynamically

```fsharp
// example of control on request and route value dictionary
    // asynchronously
    POST_ASYNC "/externalApi" (fun r rvd -> 
        task {
            // read request content and meddle here...
            return {| Success = true |} |> R_JSON 
        }
    )
```

## HTTP RESPONSE CONSTRUCTORS

Available HTTP content constructors are: 

* `R_TEXT` : returns plain text
* `R_JSON` : returns json
* `R_ERROR` : returns an HTTP error

## Configure Services HELPERS

* `WITH_SERVICES` : to override your ConfigureServices for tests
* `WITH_TEST_SERVICES`: to override your specific test services (a bit redundant in some cases, depending on the need)

## More Examples?

See examples in test folder for more details on the usage.

## ApiStub.FSharp.Stubbery

A version using the [Stubbery](https://github.com/markvincze/Stubbery) library is also present for "compatibility" when migrating from `stubbery` versions of pre existing integration tests, in your integration tests setup.

In general it's advised to not have dependencies or running any in-memory http server if possible, so the minimal version is preferred.


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

## How to Contribute

* Search for an open issue or report one, check if a similar issue was reported first
* feel free to get in touch, to fork and checkout the repo
* test and find use cases for this library, testing in F# is awesome!!!!

### References

* more info on [F# xunit testing](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-fsharp-with-dotnet-test).
* more general info on aspnetcore integration testing if you use [Nunit](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-fsharp-with-nunit) instead.
* [aspnetcore integration testing](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-7.0) docs in C#