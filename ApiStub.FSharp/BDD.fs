namespace ApiStub.FSharp

open System
open System.Net.Http
open System.Threading.Tasks
open ApiStub.FSharp.CE
open Microsoft.AspNetCore.Mvc.Testing
open ApiStub.FSharp.HttpResponseHelpers

module BDD =


    /// Defines a BDD scenario
    type Scenario<'TStartup when 'TStartup: not struct> =
        { UseCase: string
          TestWAFBuilder: TestWebAppFactoryBuilder<'TStartup> }

    /// Defines the context propagated through the test
    type Environment<'TStartup, 'FeatureStubData when 'TStartup: not struct> =
        { Scenario: Scenario<'TStartup>
          FeatureStubData: 'FeatureStubData
          Factory: 'TStartup WebApplicationFactory
          Client: HttpClient }

    /// Result of a Given gherkin clause
    type GivenResult<'ArrangeData, 'FeatureStubData, 'TStartup when 'TStartup: not struct> =
        { Environment: Environment<'TStartup, 'FeatureStubData>
          ArrangeData: 'ArrangeData } // once preconditions are set

    /// Result of a When gherkin clause
    type WhenResult<'ArrangeData, 'FeatureStubData, 'AssertData, 'TStartup when 'TStartup: not struct> =
        { Given: GivenResult<'ArrangeData, 'FeatureStubData, 'TStartup>
          AssertData: 'AssertData }

    /// Generic step type for the BDD steps
    type Step<'TStartup, 'FeatureStubData, 'ArrangeData, 'AssertData when 'TStartup: not struct> =
        | Scenario of Scenario<'TStartup>
        | Environment of Environment<'TStartup, 'FeatureStubData>
        | Given of GivenResult<'ArrangeData, 'FeatureStubData, 'TStartup>
        | When of WhenResult<'ArrangeData, 'FeatureStubData, 'AssertData, 'TStartup>
        | Invalid of error: string


    /// Scenario builder
    let SCENARIO useCase (builder: TestWebAppFactoryBuilder<_>) =
        { UseCase = useCase
          TestWAFBuilder = builder }
        |> Step.Scenario

    /// Setup the Environment for the given scenario
    let SETUP arrangeTestEnvironment customizeClient step =
        task {

            match step with
            | Step.Scenario(scenario) ->

                let! environment = arrangeTestEnvironment scenario

                let newEnv =
                    { environment with
                        Client = environment.Client |> customizeClient }

                return Step.Environment(newEnv)
            | _ -> return Step.Invalid("only environment is supported for ENVIRONMENT_SETUP")
        }

    /// specify a GIVEN gherkin clause
    let GIVEN setPreconditions stepTask =
        task {

            let! step = stepTask

            let environmentResult =
                match step with
                | Step.Environment(e) -> Result.Ok(e)
                | Step.Given(g) -> Result.Ok(g.Environment)
                | _ -> Result.Error($"{step} is not supported in GIVEN clause")

            let! (preResult: Result<_ * 'ArrangeData, string>) =
                task {
                    match environmentResult with
                    | Ok(e) ->
                        let! pre = setPreconditions e
                        return Ok(e, pre)
                    | Error(e) -> return Error(e)
                }

            return
                match preResult with
                | Result.Ok(e, pre) ->
                    let given = { ArrangeData = pre; Environment = e }

                    Step.Given(given)

                | Result.Error(e) -> Step.Invalid(e)
        }

    /// specify a WHEN gherkin clause
    let WHEN action stepTask =
        task {

            let! step = stepTask

            let givenResult =
                match step with
                | Step.Given(g) -> Result.Ok(g)
                | Step.When(w) -> Result.Ok(w.Given)
                | _ -> Result.Error($"{step} is not supported in WHEN clause")

            let! r =
                task {
                    match givenResult with
                    | Ok(g) ->

                        let! assertData = action g

                        let w = { Given = g; AssertData = assertData }
                        return Step.When(w)
                    | Error(e) -> return Step.Invalid(e)
                }

            return r
        }

    /// Specify an assert in THEN gherkin format
    let THEN assertAction stepTask =
        task {

            let! step = stepTask

            match step with
            | Step.When(w) ->
                do assertAction w
                return Step.When(w)
            | _ -> return Step.Invalid($"{step} is not supported in WHEN clause")
        }

    /// Conclude the pipeline of steps
    let END stepTask =
        task {

            let! step = stepTask

            match step with
            | Step.When(w) ->
                // dispose and return
                use d = w.Given.Environment.Client
                use dd = w.Given.Environment.Factory
                ()
            | _ -> ()
        }


// [<Fact>] sample
// let ``when i call /hello i get 'world' back with 200 ok`` (TestWebAppFactoryBuilder: TestWebAppFactoryBuilder<_>) =

//     let stubData = [ 1, 2, 3 ]

//     TestWebAppFactoryBuilder { GET "/hello" (fun _ _ -> $"hello world {stubData}" |> R_TEXT) }
//     |> SCENARIO "when i call /hello i get 'world' back with 200 ok"
//     |> SETUP
//         (fun s ->
//             task {

//                 let test = s.TestWebAppFactoryBuilder

//                 let f = test.GetFactory()

//                 return
//                     { Client = f.CreateClient()
//                       Factory = f
//                       Scenario = s
//                       FeatureStubData = stubData }
//             })
//         (fun c -> c)
//     |> GIVEN(fun g -> "hello" |> Task.FromResult)
//     |> WHEN(fun g ->
//         task {
//             let! (r: HttpResponseMessage) = g.Environment.Client.GetAsync("/Hello")
//             return! r.Content.ReadAsStringAsync()
//         })
//     |> THEN(fun w ->
//         let _ = ("hello world 1,2,3" = w.AssertData)
//         ())
//     |> END
