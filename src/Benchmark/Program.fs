// Learn more about F# at http://fsharp.org

open System
open System.Text
open System.Net
open System.Net.WebSockets
open System.Threading
open System.Net.Http

let baseAddress = new Uri ("http://localhost:8083")

let sendMessage (socket: ClientWebSocket) (cancel: CancellationToken) (message: string) = async {
    if socket.State = WebSocketState.Open then
        let messageBuffer = Encoding.UTF8.GetBytes message
        // TODO split long message
        return! socket.SendAsync(new ArraySegment<byte>(messageBuffer), WebSocketMessageType.Text, true, cancel) |> Async.AwaitTask
    else
        printfn "sendMessage: Socket is not opened"
    return ()   
}

let listenSocket (socket: ClientWebSocket) (cancel: CancellationToken) = async {
    let buffer = Array.create<byte> 1024 (byte 0)
    while socket.State = WebSocketState.Open && (not cancel.IsCancellationRequested) do
        let message = new StringBuilder()

        let mutable endOfMessage = false
        while not endOfMessage do
            let! result = socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancel) |> Async.AwaitTask
            match result.MessageType with
            | WebSocketMessageType.Close ->
                return ()
            | _ ->
                let str = Encoding.UTF8.GetString(buffer, 0, result.Count);
                message.Append str |> ignore
            endOfMessage <- result.EndOfMessage

        printfn "received: %s" (message.ToString())
                
    return ()
}

let benchmark (sessionCookies: CookieContainer) = async {

    let cts = new CancellationTokenSource()

    let socketUriBuilder = new UriBuilder(baseAddress)
    socketUriBuilder.Scheme <- "ws"
    socketUriBuilder.Path <- "/api/socket"

    let socketUri = socketUriBuilder.Uri

    let socket = new ClientWebSocket()
    socket.Options.Cookies <- sessionCookies

    let cookies = socket.Options.Cookies.GetCookies(baseAddress)
    for ci in 0..cookies.Count-1 do
        let cookie = cookies.[ci]
        printfn "Cookie[%s] = '%s'" cookie.Name cookie.Value

    try
        do! socket.ConnectAsync (socketUri, cts.Token) |> Async.AwaitTask
        printfn "Connected"
    with :? AggregateException as ae ->
        match ae.InnerExceptions.[0] with
        | :? WebSocketException as e ->
            printfn "Connection failed with error %A" e
        | x ->
            printfn "Unknown exception %A" x

    if socket.State = WebSocketState.Open then
        printfn "Socket opened"

        do listenSocket socket cts.Token |> Async.Start
        do! sendMessage socket cts.Token "\"Greets\""

        do! Async.Sleep(1000)
        do cts.Cancel()

    if socket.State = WebSocketState.Open then
        printfn "Closing..."
        do! socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", cts.Token) |> Async.AwaitTask
        printfn "Closed"

    return ()
}

let doClientSession nickName (f: CookieContainer -> unit Async) = async {

    let handler = new HttpClientHandler()
    use c = new HttpClient(handler)

    let! loginResponse =
        c.PostAsync (
            new Uri(baseAddress, "logon"),
            new FormUrlEncodedContent(dict ["nick", nickName])) |> Async.AwaitTask

    printfn "login response %A" loginResponse.StatusCode
    let! loginReplyContent = loginResponse.Content.ReadAsStringAsync() |> Async.AwaitTask

    if loginReplyContent.Contains("Register failed") then
        printf "Register failed, full message: %s" loginReplyContent
    else
        try
            return! f handler.CookieContainer
        finally
            let taskResult (task: Tasks.Task<_>) = task.Result
            let logoffResponse = c.GetAsync (new Uri(baseAddress, "logoff")) |> taskResult
            printfn "logoff response %A" logoffResponse.StatusCode
        ()

    return ()
}

[<EntryPoint>]
let main argv =
    printfn "Benchmark"

    doClientSession "bench" benchmark |> Async.RunSynchronously
    0
