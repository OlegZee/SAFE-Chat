// Learn more about F# at http://fsharp.org

open System
open System.Text
open System.Net
open System.Net.WebSockets
open System.Collections.Concurrent

open System.Threading
open System.Net.Http
open System.Diagnostics

open Newtonsoft.Json
open Argu
open FsChat

let private jsonConverter = Fable.JsonConverter() :> JsonConverter

let json message =
    JsonConvert.SerializeObject (message, [|jsonConverter|])

let unjson<'t> jsonString =
    try
        JsonConvert.DeserializeObject<'t> (jsonString, [|jsonConverter|]) |> Ok
    with e ->
        Error <| sprintf "Failed to parse message '%s': %A" jsonString e
        
let forValid f result = match result with | Ok v -> f v | _ -> ()
let (><) f a b = f b a


let rec takeUntil f (queue: ConcurrentQueue<_>) =
    let succ, m = queue.TryDequeue()
    if not succ then None
    else
        match f m with
        | Some _ as x -> x
        | _ -> takeUntil f queue

let rec fetchUntil maxWait f (queue: ConcurrentQueue<_>) = async {
    let item = queue |> takeUntil f
    match item, maxWait with
    | _, 0 -> return None
    | (Some _),_ -> return item
    | None, _ ->
        do! Async.Sleep 1
        return! fetchUntil (maxWait - 1) f queue
}

let mutable sentMessageCount = ref 0
let mutable recvMessageCount = ref 0

let sendMessage (socket: ClientWebSocket) (cancel: CancellationToken) (message: Protocol.ServerMsg) = async {
    try
        if socket.State = WebSocketState.Open then
            let messageBuffer = message |> (json >> Encoding.UTF8.GetBytes)
            // TODO split long message
            ignore <| Interlocked.Increment sentMessageCount
            return! socket.SendAsync(new ArraySegment<byte>(messageBuffer), WebSocketMessageType.Text, true, cancel) |> Async.AwaitTask
        else
            printfn "sendMessage: Socket is not opened"
    with e ->
        printfn "ERROR: Failed to send message, reason: %A" e

    return ()   
}

let listenSocket (socket: ClientWebSocket) (cancel: CancellationToken) f = async {
    let buffer = Array.create<byte> 1024 (byte 0)
    while socket.State = WebSocketState.Open && (not cancel.IsCancellationRequested) do
        let message = new StringBuilder()

        let mutable endOfMessage = false
        while not endOfMessage do
            let! result = socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancel) |> Async.AwaitTask
            ignore <| Interlocked.Increment recvMessageCount

            match result.MessageType with
            | WebSocketMessageType.Close ->
                return ()
            | _ ->
                let str = Encoding.UTF8.GetString(buffer, 0, result.Count);
                message.Append str |> ignore
            endOfMessage <- result.EndOfMessage

        let message = unjson<Protocol.ClientMsg> <| message.ToString()
        f message
                
    return ()
}

let withApiSocket (baseAddress: Uri) (sessionCookies: CookieContainer) (cancel: CancellationToken) f = async {

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
        do! f socket cancel

    if socket.State = WebSocketState.Open then
        printfn "Closing...(state: %A)" socket.State
        do! socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", cancel) |> Async.AwaitTask
        printfn "Closed"

    return ()
}

let doClientSession (baseAddress: Uri) nickName (f: CookieContainer -> unit Async) = async {

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


let createChannel (socket: ClientWebSocket) queue cancelToken name = async {
    let hasJoined waitForReqId = function
        | Protocol.CmdResponse(reqId, Protocol.JoinedChannel info) when reqId = waitForReqId -> Some info
        | _ -> None

    let requestId = System.Guid.NewGuid().ToString()
    do! sendMessage socket cancelToken (Protocol.ServerCommand (requestId, Protocol.JoinOrCreate name))
    return! queue |> fetchUntil 1000 (hasJoined requestId)
}

// Flood benchmark
let flood channelCount messagePerChannelCount =
    printfn "Flood benchmark"

    let body socket cancelToken = async {

        let queue = new ConcurrentQueue<Protocol.ClientMsg>()

        do listenSocket socket cancelToken (forValid queue.Enqueue) |> Async.Start

        do! sendMessage socket cancelToken Protocol.ServerMsg.Greets
        let! hello = queue |> fetchUntil 1000 (function | Protocol.Hello x -> Some x | _ -> None)
        match hello with
        | Some { me = { id = userid; nick = name} } ->
            printfn "Connected '%s' under id=%A" name userid
            printfn "Creating channels"
            let mutable channels = []
            for chanIndex in [1..channelCount] do
                let chanName = sprintf "chan-%i" chanIndex
                let! chanInfo = createChannel socket queue cancelToken chanName
                match chanInfo with
                | Some chan -> channels <- chan :: channels
                | None -> printfn "Failed creating channel %s" chanName

            printfn "%s: Sending %i messages to each of %i channels" userid messagePerChannelCount channelCount

            let stopwatch = Stopwatch.StartNew()

            let mutable messageCount = 0
            for _ in [1..messagePerChannelCount] do
                for channel in channels do
                    do! sendMessage socket cancelToken (Protocol.ServerMsg.UserMessage {text = "hello"; chan = channel.id})
                    messageCount <- messageCount + 1
                    if messageCount % 1000 = 0 then
                        printfn "%s: %i messages sent" userid messageCount

                // using ping-pong to make sure all messages are processed
                let pingId = System.DateTime.Now.ToString()
                
                do! sendMessage socket cancelToken (Protocol.ServerMsg.ServerCommand (pingId, Protocol.Ping))
                let ispong = function | Protocol.CmdResponse (reqid, Protocol.Pong) when reqid = pingId -> Some () | _ -> None

                let! pong = queue |> fetchUntil 1000 ispong
                if Option.isNone pong then
                    printfn "ERROR: failed to get pong"

            stopwatch.Stop()

            printfn "%s: Send %i messages in %A" userid messageCount stopwatch.Elapsed

            do! Async.Sleep(100)

        | _ -> printfn "Handshake failed"
    }

    body

type Arguments =
    | Login of nick: string
    | ChannelCount of count: int
    | MessageCount of count: int
    | Server of host: string
    | UserCount of count: int
with
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Login _ -> "The nickname to run benchmark under"
            | ChannelCount _ -> "Number of channels to create"
            | MessageCount _ -> "Number of messages to sent to each channel"
            | Server _ -> "Server address"
            | UserCount _ -> "Number of simultaneous sessions"

let parser = ArgumentParser.Create<Arguments>(programName = "benchmark.exe")


[<EntryPoint>]
let main argv =
    printfn "Benchmark"
    try
        let results = parser.ParseCommandLine(inputs = argv, raiseOnUsage = true)

        let baseAddress = results.GetResult(<@ Server @>, defaultValue = "http://localhost:8083")
        let nick = results.GetResult(<@ Login @>, defaultValue = "bench")
        let channelCount = results.GetResult(<@ ChannelCount @>, defaultValue = 10)
        let messageCount = results.GetResult(<@ MessageCount @>, defaultValue = 100)
        let sessionCount = results.GetResult(<@ UserCount @>, defaultValue = 1)

        let cts = new CancellationTokenSource()

        let session nickName =
            doClientSession (new Uri(baseAddress)) nickName (fun cookies -> withApiSocket (new Uri(baseAddress)) cookies cts.Token (flood channelCount messageCount))
        let bench =
            match sessionCount with
            | n when n > 1 ->
                [for sessionIdx in [1..n] -> session (sprintf "%s-%i" nick sessionIdx)] |> (Async.Parallel >> Async.Ignore)
            | _ -> session nick

        bench |> Async.RunSynchronously |> ignore

        cts.Cancel()

        printfn "Stats: sent:%i received: %i" (!sentMessageCount) (!recvMessageCount)

        0

    with e ->
        printfn "%s" e.Message
        0

    // Async.Parallel