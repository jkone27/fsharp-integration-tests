namespace fsharpintegrationtests.Controllers

open System
open System.Collections.Generic
open System.Linq
open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Logging
open fsharpintegrationtests
open System.Threading.Tasks
open System.Net.Http

[<ApiController>]
[<Route("[controller]")>]
type WeatherForecastController(logger: ILogger<WeatherForecastController>) =
    inherit ControllerBase()

    let summaries =
        [| "Freezing"
           "Bracing"
           "Chilly"
           "Cool"
           "Mild"
           "Warm"
           "Balmy"
           "Hot"
           "Sweltering"
           "Scorching" |]

    [<HttpGet>]
    member _.Get() =
        let rng = System.Random()

        [| for index in 0..4 ->
               { Date = DateTime.Now.AddDays(float index)
                 TemperatureC = rng.Next(-20, 55)
                 Summary = summaries.[rng.Next(summaries.Length)] } |]


[<ApiController>]
[<Route("[controller]")>]
type HelloController(logger: ILogger<WeatherForecastController>, httpClientFactory: IHttpClientFactory) =

    inherit ControllerBase()

    [<HttpGet>]
    member _.GetAsync() =
        task {

            let httpClient = httpClientFactory.CreateClient("externalApiClient")

            let! res = httpClient.GetAsync("/externalApi")

            res.EnsureSuccessStatusCode() |> ignore

            return! res.Content.ReadAsStringAsync()
        }
