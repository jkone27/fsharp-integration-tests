(*** hide ***)
#r "nuget: FSharp.Formatting, 11.3.0"

(**
## BDD (gherkin) Extensions 🥒

You can use some BDD extension to perform [Gherkin-like setups and assertions](https://cucumber.io/docs/gherkin/reference/)

they are all async `task` computations so they can be simply chained together:

* `SCENARIO`: takes a `TestClient<TStartup>` as input and needs a name for your test scenario
* `SETUP`: takes a scenario as input and can be used to configure the "test environmenttest": factory and the API client, additionally takes a separate API client configuration
* `GIVEN`: takes a "test environment" or another "given" and returns a "given" step
* `WHEN`: takes a "given" or another "when" step, and returns a a "when" step
* `THEN`: takes a "when" step and asserts on it, returns the same "when" step as result to continue asserts
* `END`: disposes the "test environment" and concludes the task computation

```fsharp
// open ...
open ApiStub.FSharp.BDD
open HttpResponseMessageExtensions

module BDDTests =

    let testce = new TestClient<Startup>()

    [<Fact>]
    let ``when i call /hello i get 'world' back with 200 ok`` () =
            
            let mutable expected = "_"
            let stubData = { Ok = "undefined" }

            // ARRANGE step is divided in CE (arrange client stubs)
            // SETUP: additional factory or service or client configuration
            // and GIVEN the actual arrange for the test 3As.
                
            // setup your test as usual here, test_ce is an instance of TestClient<TStartup>()
            test_ce {
                POSTJ "/another/anotherApi" {| Test = "NOT_USED_VAL" |}
                GET_ASYNC "/externalApi" (fun r _ -> task { 
                    return { stubData with Ok = expected } |> R_JSON 
                })
            }
            |> SCENARIO "when i call /Hello i get 'world' back with 200 ok"
            |> SETUP (fun s -> task {
            
                let test = s.TestClient
                
                // any additiona services or factory configuration before this point
                let f = test.GetFactory() 
                
                return {
                    Client = f.CreateClient()
                    Factory = f
                    Scenario = s
                    FeatureStubData = stubData
                }
            }) (fun c -> c) // configure test client here if needed
            |> GIVEN (fun g -> //ArrangeData
                expected <- "world"
                expected |> Task.FromResult
            )
            |> WHEN (fun g -> task { //ACT and AssertData
                let! (r : HttpResponseMessage) = g.Environment.Client.GetAsync("/Hello")
                return! r.Content.ReadFromJsonAsync<Hello>()

            })
            |> THEN (fun w -> // ASSERT
                Assert.Equal(w.Given.ArrangeData, w.AssertData.Ok) 
            )
            |> END

```
