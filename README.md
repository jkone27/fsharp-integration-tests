# ApiStub.FSharp [![NuGet Badge](https://img.shields.io/nuget/v/ApiStub.FSharp)](https://www.nuget.org/packages/ApiStub.FSharp) 🦔

<img width="331" alt="Screenshot 2024-12-16 at 13 01 33" src="https://github.com/user-attachments/assets/5095d6cb-63bb-4644-836f-99b5355870fe" />


[![Ceasefire Now](https://badge.techforpalestine.org/ceasefire-now)](https://techforpalestine.org/learn-more)  
[![Stand With Ukraine](https://raw.githubusercontent.com/vshymanskyy/StandWithUkraine/main/badges/StandWithUkraine.svg)](https://stand-with-ukraine.pp.ua)  
<a href='https://juststopoil.org/' target="_blank"><img alt='JUST_STOP_OIL' src='https://img.shields.io/badge/Just_STOP OIL-100000?style=plastic&logo=JUST_STOP_OIL&logoColor=white&labelColor=FFA600&color=000000'/></a>  

## Easy API Testing 🧞‍♀️

This library makes use of [F# computation expressions](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/computation-expressions) to hide some complexity of `WebApplicationFactory` and provide the user with a *domain specific language* (DSL) for integration tests. 

A C# API is also available, access the [documentation](https://jkone27.github.io/fsharp-integration-tests/) website for more info on how to use this library.  

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

            let client = 
                test { 
                    GETJ "/externalApi" {| Ok = "yeah" |}
                }
                |> _.GetFactory()
                |> _.CreateClient()

            let! r = client.GetAsync("/Hello")

        }
```

### Test .NET C# 🤝 from F#

F# is a great language, but it doesn't have to be scary to try it. Integration and Unit tests are a great way to introduce F# to your team if you are already using .NET or ASPNETCORE. 

In fact you can add an `.fsproj` within a C# aspnetcore solution `.sln`, and just have a single F# assembly test your C# application from F#, referencing a `.csproj` file is easy! just use regular [dotnet add reference command](https://learn.microsoft.com/bs-latn-ba/dotnet/core/tools/dotnet-add-reference).

## How to Contribute ✍️

* Search for an open issue or report one, and check if a similar issue was reported first
* feel free to get in touch, to fork and check out the repo
* test and find use cases for this library, testing in F# is awesome!!!!

### References

* more info on [F# xunit testing](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-fsharp-with-dotnet-test).
* more general info on aspnetcore integration testing if you use [Nunit](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-fsharp-with-nunit) instead.
* [aspnetcore integration testing](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-7.0) docs in C#

