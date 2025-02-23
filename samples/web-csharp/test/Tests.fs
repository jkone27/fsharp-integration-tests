module Web.CSharp.Tests.WithCe

open System
open Xunit
open ApiStub.FSharp
open ApiStub.FSharp.CE
open System.Net.Http.Json

let ce = (new TestClient<Program>()) {
    GETJ "persons" [{| Name = "John" ; Age = 30 |}]
}

[<Fact>]
let ``Test with CE and HTTP mocking`` () =
    task {
        use f = ce.GetFactory()
        let c = f.CreateClient()

        let! r = c.GetFromJsonAsync<{| Age: int|}>("/john")

        Assert.Equal(30, r.Age)
    }
