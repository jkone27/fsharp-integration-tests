#i """nuget:/Users/admin/Repositories/fsharp-integration-tests/ApiStub.FSharp/bin/Debug/"""
#r "nuget:ApiStub.FSharp, 1.0.1-alpha"

open ApiStub.FSharp

module Test = 
    open CE
    open BuilderExtensions
    open HttpResponseHelpers
    open System.Threading.Tasks

    // build your aspnetcore integration testing CE
    let test () = new CE.TestClient<Startup>()

    let ``test`` () =
            task {

                let testApp =
                    test () { 
                        GETJ "/externalApi" {| Ok = "yeah" |}
                    }

                use client = testApp.GetFactory().CreateClient()

                let! r = client.GetAsync("/Hello")

                r.EnsureSuccessStatusCode()
            } 