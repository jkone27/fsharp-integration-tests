namespace fsharpintegrationtests

open Microsoft.Net.Http.Headers

#nowarn "20"
open System
open System.Collections.Generic
open System.IO
open System.Linq
open System.Threading.Tasks
open Microsoft.AspNetCore
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.HttpsPolicy
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Swashbuckle.AspNetCore

// integration tests in fsharp rely on this atm
type Startup(configuration: IConfiguration, env: IWebHostEnvironment) =

    abstract member ConfigureServices: IServiceCollection -> unit
    default this.ConfigureServices(services: IServiceCollection) =

        services.AddControllers()

        services.AddHttpClient(fun httpClient ->
            httpClient.BaseAddress <- new Uri("https://www.google.com/")
        )

        services.AddEndpointsApiExplorer()
        services.AddSwaggerGen()
        ()
        
    abstract member Configure: IApplicationBuilder -> unit
    default this.Configure(app: IApplicationBuilder) =
        
        if env.IsDevelopment() then
            app.UseSwagger()
            app.UseSwaggerUI()
            ()

        app.UseHttpsRedirection()

        app.UseAuthorization()

        app.UseRouting()
        app.UseEndpoints(fun o -> o.MapControllers() |> ignore)
        ()


module public Program =

    let createHost (args : string []) =
        let builder = WebApplication.CreateBuilder(args)

        let st = new Startup(builder.Configuration, builder.Environment)

        st.ConfigureServices(builder.Services)

        let app = builder.Build()

        st.Configure(app)

        app

    [<EntryPoint>]
    let main args =

        let app = createHost args

        app.Run()

        0 //exit
