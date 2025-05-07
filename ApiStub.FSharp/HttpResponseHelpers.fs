namespace ApiStub.FSharp

open System.Net.Http
open System
open System.Net
open System.Net.Http.Json
open System.Text.Json

module HttpResponseHelpers =

    let inline R_OK contentType content =
        let response = new HttpResponseMessage(HttpStatusCode.OK)
        response.Content <- new StringContent(content, Text.Encoding.UTF8, contentType)
        response

    let inline R_TEXT content = content |> R_OK "text/html"

    let inline R_JSON (x: obj) =
        x |> JsonSerializer.Serialize |> R_OK "application/json"

    let inline R_JSON_CONTENT (x: obj) =
        // this seems to be buggy, causing stream read exception with F# types!!
        let response = new HttpResponseMessage(HttpStatusCode.OK)
        response.Content <- JsonContent.Create(inputValue = x)
        response

    let inline R_ERROR statusCode content =
        let response = new HttpResponseMessage(statusCode)
        response.Content <- content
        response


module HttpResponseMessageExtensions =

    type HttpResponseMessage with

        member this.EnsureSuccessOrFailWithContent() =
            task {
                let! contentString = this.Content.ReadAsStringAsync()

                if this.IsSuccessStatusCode |> not then
                    if contentString |> String.IsNullOrWhiteSpace then
                        failwith
                            $"{this.RequestMessage.Method} {this.RequestMessage.RequestUri.AbsolutePath}: unknown server error {this.StatusCode}"
                    else
                        failwith contentString

                return ()
            }

        member this.EnsureSuccessAndParse<'a>() =
            task {
                let! contentString = this.Content.ReadAsStringAsync()

                if this.IsSuccessStatusCode |> not then
                    if contentString |> String.IsNullOrWhiteSpace then
                        failwith
                            $"{this.RequestMessage.Method} {this.RequestMessage.RequestUri.AbsolutePath}: unknown server error {this.StatusCode}"
                    else
                        failwith contentString

                return contentString |> JsonSerializer.Deserialize<'a>
            }
