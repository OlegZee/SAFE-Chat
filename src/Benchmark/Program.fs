// Learn more about F# at http://fsharp.org

open System
open System.Net.WebSockets
open System.Threading
open System.Net.Http

let benchmark () = async {

    let baseAddress = new Uri ("http://localhost:8083")
    let handler = new HttpClientHandler()
    use c = new HttpClient(handler)

    let content = new System.Net.Http.FormUrlEncodedContent(dict ["nick", "bench"])
    let! loginResponse = c.PostAsync(new Uri(baseAddress, "logon"), content) |> Async.AwaitTask

    printfn "login response %A" loginResponse.StatusCode

    let cts = new CancellationTokenSource()
    let uri = new Uri("ws://localhost:8083/api/socket", UriKind.Absolute)

    let client = new ClientWebSocket()
    client.Options.Cookies <- handler.CookieContainer

    let cookies = client.Options.Cookies.GetCookies(baseAddress)
    for ci in 0..cookies.Count-1 do
        let cookie = cookies.[ci]
        printfn "Cookie[%s] = '%s'" cookie.Name cookie.Value

    try
        do! client.ConnectAsync (uri, cts.Token) |> Async.AwaitTask
        printfn "Connected"
    with :? AggregateException as ae ->
        match ae.InnerExceptions.[0] with
        | :? WebSocketException as e ->
            printfn "Connection failed with error %A" e
        | x ->
            printfn "Unknown exception %A" x

    if client.State = WebSocketState.Open then
        ()

    return 0
}

[<EntryPoint>]
let main argv =
    printfn "Benchmark"

    benchmark() |> Async.RunSynchronously
