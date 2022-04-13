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
open System.Net.Http.Json

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

    // https://stackoverflow.com/questions/23438416/why-is-httpclient-baseaddress-not-working

    [<HttpGet>]
    member _.GetAsync() =
        task {

            let externalApiClient = httpClientFactory.CreateClient("externalApiClient")

            let! res = externalApiClient.GetAsync("externalApi")

            res.EnsureSuccessStatusCode() |> ignore

            let anotherApiClient = httpClientFactory.CreateClient("anotherApiClient") 

            let! res2 = anotherApiClient.PostAsJsonAsync("anotherApi?test=123", {| Test="Ok" |})
                
            res2.EnsureSuccessStatusCode() |> ignore

            let! str = res2.Content.ReadAsStringAsync()

            let! res2 = res.Content.ReadFromJsonAsync<Hello>()

            return res2
        }
