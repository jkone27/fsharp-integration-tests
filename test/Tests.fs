namespace test

open Xunit
open fsharpintegrationtests
open Microsoft.AspNetCore.Mvc.Testing
open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.Hosting
open SwaggerProvider
open System.Threading.Tasks
open System.Net.Http
open System

type MyOpenapi = OpenApiClientProvider<"swagger.json">

module CE =

    //because NET6 is fucked :) 
    type TestStartup(c,e) as this = 
        inherit Startup(c,e)

        static member val HttpClientBaseAddress = "" with get, set

        override this.ConfigureServices(services: IServiceCollection) =
            base.ConfigureServices(services)
            services.AddHttpClient(fun c -> 
                c.BaseAddress <- new Uri(TestStartup.HttpClientBaseAddress, UriKind.Absolute)
                )
            ()

        override this.Configure(app) =
            base.Configure(app)
            ()

    type TestFactory<'T when 'T : not struct>() as this = 
        inherit WebApplicationFactory<'T>()

        /// bool return parameter : continueBase
        member val WithHttpClient: HttpClient -> bool = fun _ -> false with get,set

        member val WithBuilder: IWebHostBuilder -> bool = fun _ -> false with get,set

        override this.ConfigureClient(httpClient) =
            if this.WithHttpClient(httpClient) then
                base.ConfigureClient(httpClient)

        override this.ConfigureWebHost(builder) =
            if this.WithBuilder(builder) then
                base.ConfigureWebHost(builder)
    
    type TestClient<'T when 'T : not struct>() as this =

       let factory = new TestFactory<'T>()
       let stubbery = new Stubbery.ApiStub()

       member this.Yield(()) = (factory, stubbery)

       [<CustomOperation("builder")>]
       member this.Build(_, builder: IWebHostBuilder -> bool) =
           factory.WithBuilder <- builder
           this

       [<CustomOperation("test_client")>]
       member this.TestClient(_, builder: HttpClient -> bool) =
           factory.WithHttpClient <- builder
           this

       [<CustomOperation("stub_port")>]
       member this.StubPort(_, port: int) =
           stubbery.Port <- port
           this

       [<CustomOperation("stub")>]
       member this.Stub(_, stub: Stubbery.ApiStub -> Stubbery.ISetup) =
           stub(stubbery)|> ignore
           this

       member this.CreateTestClient() = 
           stubbery.Start()
           TestStartup.HttpClientBaseAddress <- stubbery.Address
           factory.WithWebHostBuilder(fun b -> b.UseStartup<TestStartup>() |> ignore).CreateDefaultClient()

       member val Services = factory.Services

    // CE builder
    let test = 
        new TestClient<Startup>() 

module Tests =

    open CE

    [<Fact>]
    let ``GET weather returns a not null Date`` () =
    
        let application = 
            (new WebApplicationFactory<Startup>())
                .WithWebHostBuilder(fun b -> 
                    //
                    //b.
                    ()
                )

        let client = application.CreateClient()

        let typedClient = MyOpenapi.Client(client)

        task {
            
            let! forecast = typedClient.GetWeatherForecast()

            Assert.NotNull(forecast[0].Date)
        }
     
    [<Fact>]
    let anothertest () =
        let app = test {
                stub (fun s -> s.Get("/externalApi", 
                    fun r args -> 
                        ({| Ok = "yeah" |} |> box)))
            }

        let client = app.CreateTestClient()

        let typedClient = MyOpenapi.Client(client)


        task {

            let! r = client.GetAsync("/hello")

            let! rr = r.EnsureSuccessStatusCode().Content.ReadAsStringAsync()

            let! response = typedClient.GetHello()

            Assert.NotEmpty(response)

        }
