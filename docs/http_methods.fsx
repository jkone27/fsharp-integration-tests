(*** hide ***)
#r "nuget: FSharp.Formatting, 11.3.0"

(**
## HTTP Methods 🚕

Available HTTP methods in the test DSL to "mock" HTTP client responses are the following:

### Basic

* `GET`, `PUT`, `POST`, `DELETE` - for accessing request, route parameters and sending back HttpResponseMessage (e.g. using R_JSON or other constructors)

```fsharp
    // example of control on request and route value dictionary
    PUT "/externalApi" (fun r rvd -> 
        // read request properties or route, but not content...
        // unless you are willing to wait the task explicitly as result
        {| Success = true |} |> R_JSON 
    )
```

### JSON 📒

* `GETJ`, `PUTJ`, `POSTJ`, `DELETEJ` - for objects converted to JSON content

```fsharp
GETJ "/yetAnotherOne" {| Success = true |}
```

### ASYNC Overloads (task) ⚡️

* `GET_ASYNC`, `PUT_ASYNC`, `POST_ASYNC`, `DELETE_ASYNC` - for handling asynchronous requests inside a task computation expression (async/await) and mock dynamically

```fsharp
// example of control on request and route value dictionary
    // asynchronously
    POST_ASYNC "/externalApi" (fun r rvd -> 
        task {
            // read request content and meddle here...
            return {| Success = true |} |> R_JSON 
        }
    )
```

### HTTP response helpers 👨🏽‍🔧

Available HTTP content constructors are: 

* `R_TEXT`: returns plain text
* `R_JSON`: returns JSON
* `R_ERROR`: returns an HTTP error
