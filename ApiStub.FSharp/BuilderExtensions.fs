namespace ApiStub.FSharp

open Microsoft.AspNetCore.Mvc.Testing
open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.Hosting
open System.Threading.Tasks
open System
open Microsoft.AspNetCore.TestHost

module BuilderExtensions =

    let configure_services (configure : IServiceCollection -> 'a) (builder: IWebHostBuilder) : IWebHostBuilder =
        builder.ConfigureServices(fun s -> configure(s) |> ignore)

    let configure_test_services (configure : IServiceCollection -> 'a) (builder: IWebHostBuilder) : IWebHostBuilder =
        builder.ConfigureTestServices(fun s -> configure(s) |> ignore)

    let web_host_builder (builder : IWebHostBuilder -> 'a) (factory: WebApplicationFactory<'T>)   =
        factory.WithWebHostBuilder(fun b -> builder(b) |> ignore)

    let web_configure_services configure =
        configure_services configure 
        |> web_host_builder

    let web_configure_test_services configure =
        configure_test_services configure
        |> web_host_builder
