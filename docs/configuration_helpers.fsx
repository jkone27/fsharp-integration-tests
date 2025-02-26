(*** hide ***)
#r "nuget: FSharp.Formatting, 11.3.0"

(**
## Configuration helpers 🪈

* `WITH_SERVICES`: to override your ConfigureServices for tests
* `WITH_TEST_SERVICES`: to override your specific test services (a bit redundant in some cases, depending on the need)

### Example

Here is an example of how to use the configuration helpers:

```fsharp
open ApiStub.FSharp.CE
open ApiStub.FSharp.BuilderExtensions
open ApiStub.FSharp.HttpResponseHelpers
open Xunit

type ISomeSingleton =
    interface
    end

type SomeSingleton(name: string) =
    class
        interface ISomeSingleton
    end

module Tests =

    let test = new TestClient<Startup>()

    [<Fact>]
    let ``WITH_SERVICES registers correctly`` () =
        task {

            let testApp =
                test {
                    GETJ "hello" {| ResponseCode = 1001 |}

                    WITH_SERVICES(fun (s: IServiceCollection) ->
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
    let ``WITH_TEST_SERVICES registers correctly`` () =
        task {

            let singletonMock = { new ISomeSingleton }

            let testApp =
                test {
                    GETJ "hello" {| ResponseCode = 1001 |}

                    WITH_SERVICES(fun (s: IServiceCollection) ->
                        s.AddSingleton<ISomeSingleton>(new SomeSingleton("John")))

                    WITH_TEST_SERVICES(fun s -> s.AddSingleton(singletonMock))
                }

            let fac = testApp.GetFactory()

            let singleton = fac.Services.GetRequiredService<ISomeSingleton>()

            Assert.NotNull(singleton)
            Assert.Same(singletonMock, singleton)

            let client = fac.Services.GetRequiredService<HttpClient>()

            let! resp = client.GetFromJsonAsync<{| ResponseCode: int |}>("hello")

            Assert.Equal(1001, resp.ResponseCode)
        }
```
