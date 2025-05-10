namespace ApiStub.Fsharp.CSharp

open System.Runtime.CompilerServices
open ApiStub.FSharp.CE

[<Extension>]
type CsharpExtensions =

    [<Extension>]
    static member GETJ<'a when 'a: not struct>(x: TestClient<'a>, route: string, stub: obj) = x.GetJson(x, route, stub)

    [<Extension>]
    static member POSTJ<'a when 'a: not struct>(x: TestClient<'a>, route: string, stub: obj) =
        x.PostJson(x, route, stub)

    [<Extension>]
    static member PUTJ<'a when 'a: not struct>(x: TestClient<'a>, route: string, stub: obj) = x.PutJson(x, route, stub)

    [<Extension>]
    static member DELETEJ<'a when 'a: not struct>(x: TestClient<'a>, route: string, stub: obj) =
        x.DeleteJson(x, route, stub)

    [<Extension>]
    static member PATCHJ<'a when 'a: not struct>(x: TestClient<'a>, route: string, stub: obj) =
        x.PatchJson(x, route, stub)
