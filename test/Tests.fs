namespace test

open Xunit
open fsharpintegrationtests
open Microsoft.AspNetCore.Mvc.Testing
open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.Hosting
open SwaggerProvider
open System.Threading.Tasks

type MyOpenapi = OpenApiClientProvider<"swagger.json">

module Tests =

    [<Fact>]
    let ``GET weather returns a not null Date`` () =
    
        let application = 
            new WebApplicationFactory<Startup>()

        let client = application.CreateClient()

        let typedClient = MyOpenapi.Client(client)

        task {
            
            let! forecast = typedClient.GetWeatherForecast()

            Assert.NotNull(forecast[0].Date)
        }
