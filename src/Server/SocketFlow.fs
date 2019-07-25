module SocketFlow

open System
open System.Text
open System.Net.WebSockets
open Microsoft.Extensions.Logging

open FSharp.Control.Tasks.ContextInsensitive

open Akka.Actor
open Akka.Streams
open Akka.Streams.Dsl
open Akkling
open Akkling.Streams

type WsMessage =
    | Text of string
    | Data of byte array
    | Ignore

// Provides websocket handshaking. Connects web socket to a pair of Source and Sync.
// 'materialize'
let handleWebsocketMessages (system: ActorSystem) (logger: ILogger)
    (materialize: IMaterializer -> Source<WsMessage, Akka.NotUsed> -> Sink<WsMessage, _> -> unit) (socket : WebSocket) _
    =
    let materializer = system.Materializer()
    let sourceActor, inputSource =
        Source.actorRef OverflowStrategy.Fail 1000 |> Source.toMat Sink.publisher Keep.both
        |> Graph.run materializer |> fun (actor, pub) -> actor, Source.FromPublisher pub

    let emptyData = new ArraySegment<byte>()
    let asyncIgnore: Threading.Tasks.Task -> _ = Async.AwaitTask >> (fun v -> async.Bind(v, fun () -> async.Return Ignore))

    // sink for flow that sends messages to websocket
    let sinkBehavior (ctx: Actor<_>): WsMessage -> _ =
        function
        | Text text ->
            // using pipeTo operator just to wait for async send operation to complete
            let data = Encoding.UTF8.GetBytes(text) |> ArraySegment<byte>
            socket.SendAsync(data, WebSocketMessageType.Text, true, Threading.CancellationToken.None)
            |> asyncIgnore |!> ctx.Self
        | Data bytes ->
            // ws.send Binary (ByteSegment bytes) true |> asyncIgnore |!> ctx.Self
            socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Binary, true, Threading.CancellationToken.None)
            |> asyncIgnore |!> ctx.Self
        | Ignore -> ()
        >> ignored

    let sinkActor =
        props <| actorOf2 sinkBehavior |> (spawn system null) |> retype

    let sink: Sink<WsMessage,_> = Sink.ActorRef(untyped sinkActor, PoisonPill.Instance)
    do materialize materializer inputSource sink

    task {
        let mutable loop = true
        let dataBuffer = Array.create<byte> 1000 (byte 0)
        while loop do
            let! msg = socket.ReceiveAsync(dataBuffer |> ArraySegment, Threading.CancellationToken.None)
            match msg.MessageType with
            | WebSocketMessageType.Text ->
                let str = Encoding.UTF8.GetString dataBuffer
                sourceActor <! Text str
            // | (Ping, _, _) ->
            //     do! ws.send Pong emptyData true
            | WebSocketMessageType.Close ->
                logger.LogDebug ("Received Opcode.Close, terminating actor")
                (retype sourceActor) <! PoisonPill.Instance

                do! socket.SendAsync(emptyData, WebSocketMessageType.Close, true, Threading.CancellationToken.None)
                // this finalizes the Source
                loop <- false
            | _ -> ()
    }

/// Creates socket handshaking handler
let handleWebsocketMessagesFlow (system: ActorSystem) (logger) (handler: Flow<WsMessage, WsMessage, Akka.NotUsed>) (ws : WebSocket) =
    let materialize materializer inputSource sink =
        inputSource |> Source.via handler |> Source.runWith materializer sink |> ignore
    handleWebsocketMessages system logger materialize ws
