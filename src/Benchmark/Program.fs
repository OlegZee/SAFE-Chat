// Learn more about F# at http://fsharp.org

open System
open System.Text
open System.Net
open System.Net.WebSockets
open System.Threading
open System.Net.Http

open Newtonsoft.Json
open FsChat

let baseAddress = new Uri ("http://localhost:8083")

let private jsonConverter = Fable.JsonConverter() :> JsonConverter

let json message =
    JsonConvert.SerializeObject (message, [|jsonConverter|])

let unjson<'t> jsonString =
    try
        JsonConvert.DeserializeObject<'t> (jsonString, [|jsonConverter|]) |> Ok
    with e ->
        Error <| sprintf "Failed to parse message '%s': %A" jsonString e
        

let sendMessage (socket: ClientWebSocket) (cancel: CancellationToken) (message: Protocol.ServerMsg) = async {
    try
        if socket.State = WebSocketState.Open then
            let messageBuffer = message |> (json >> Encoding.UTF8.GetBytes)
            // TODO split long message
            return! socket.SendAsync(new ArraySegment<byte>(messageBuffer), WebSocketMessageType.Text, true, cancel) |> Async.AwaitTask
        else
            printfn "sendMessage: Socket is not opened"
    with e ->
        printfn "ERROR: Failed to send message, reason: %A" e

    return ()   
}

let listenSocket (socket: ClientWebSocket) (cancel: CancellationToken) limit f = async {
    let buffer = Array.create<byte> 1024 (byte 0)
    let mutable messagesToStop = limit
    while socket.State = WebSocketState.Open && (not cancel.IsCancellationRequested) && messagesToStop > 0 do
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

        let message = unjson<Protocol.ClientMsg> <| message.ToString()
        f message
        messagesToStop <- messagesToStop - 1
                
    return ()
}

let withApiSocket (sessionCookies: CookieContainer) (cancel: CancellationToken) f = async {

    let socketUriBuilder = new UriBuilder(baseAddress)
    socketUriBuilder.Scheme <- "ws"
    socketUriBuilder.Path <- "/api/socket"

    let socketUri = socketUriBuilder.Uri

    let socket = new ClientWebSocket()
    socket.Options.Cookies <- sessionCookies

    try
        do! socket.ConnectAsync (socketUri, cancel) |> Async.AwaitTask
        printfn "Connected"
    with :? AggregateException as ae ->
        match ae.InnerExceptions.[0] with
        | :? WebSocketException as e ->
            printfn "Connection failed with error %A" e
        | x ->
            printfn "Unknown exception %A" x

    if socket.State = WebSocketState.Open then
        printfn "Running benchmark"
        do! f socket

    if socket.State = WebSocketState.Open then
        printfn "Closing...(state: %A)" socket.State
        do! socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", cancel) |> Async.AwaitTask
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

let createChannels (socket: ClientWebSocket) names = async {
    let cts = new CancellationTokenSource()
    let channelList = new System.Collections.Generic.List<Protocol.ChannelInfo>()

    let collectChannelInfoReply = function
        | Ok(Protocol.ClientMsg.NewChannel info)
        | Ok(Protocol.ClientMsg.JoinedChannel info)
            -> channelList.Add info
        | Ok(Protocol.ClientMsg.UserEvent _)
            -> ()
        | m -> printfn "ignored message %A" m

    printfn "Creating channels"
    do listenSocket socket cts.Token 1000 collectChannelInfoReply |> Async.Start

    for name in names do
        do! sendMessage socket cts.Token (Protocol.ServerMsg.JoinOrCreate name)
        do! Async.Sleep(100)

    let mutable itemCount = 0
    while channelList.Count <> List.length names && itemCount < 10 do
        printfn "Waiting for all channels to be created"
        do! Async.Sleep(1000)
        itemCount <- itemCount + 1

    do cts.Cancel()

    if channelList.Count <> List.length names then
        printfn "ERROR: Only %i channels were created" channelList.Count

    return channelList |> List.ofSeq
}

[<EntryPoint>]
let main argv =
    printfn "Benchmark"

    let cts = new CancellationTokenSource()

    let body socket = async {
        do listenSocket socket cts.Token 1 (printfn "received: %A") |> Async.Start
        do! sendMessage socket cts.Token Protocol.ServerMsg.Greets
        do! Async.Sleep(100)

        printfn "Creating channels"
        let channelNames = [0..10] |> List.map (sprintf "chan-%i")
        let! channels = createChannels socket channelNames

        printfn "Sending message to all channels"
        do listenSocket socket cts.Token 1000 (fun _ -> printf "r") |> Async.Start
        for channel in channels do
            do! sendMessage socket cts.Token (Protocol.ServerMsg.UserMessage {text = "hello"; chan = channel.id})

        do! Async.Sleep(1000)
    }

    async {
        do! doClientSession "bench" (fun cookies -> withApiSocket cookies cts.Token body)
        cts.Cancel()
        return 0
    } |> Async.RunSynchronously
