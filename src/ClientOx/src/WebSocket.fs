module WebSocket

open Fable.Core
open Fable.Core.JsInterop
open Elmish
open Thoth.Json

// WebSocket connection state
type ConnectionState =
    | Disconnected
    | Connecting
    | Connected of obj
    | Error of string

type SocketHandle = {
    connectionState: ConnectionState
    url: string
}

// WebSocket events
type SocketEvent =
    | ConnectionOpened of SocketHandle
    | ConnectionClosed
    | ConnectionError of string
    | MessageReceived of string
    | MessageSent of string

// Create WebSocket connection  
let createSocket (url: string) (onEvent: SocketEvent -> unit) : SocketHandle =
    let ws: obj = emitJsExpr url "new WebSocket($0)"
    let socketHandle = {
        connectionState = Connected ws
        url = url
    }
    
    // Set up event handlers using dynamic property assignment
    ws?onopen <- (fun _ -> onEvent (ConnectionOpened socketHandle))
    ws?onclose <- (fun _ -> onEvent ConnectionClosed)
    ws?onerror <- (fun _ -> onEvent (ConnectionError "WebSocket error"))
    ws?onmessage <- (fun event -> onEvent (MessageReceived event?data))
    
    socketHandle

// Send message through WebSocket
let sendMessage (socket: SocketHandle) (message: string) : unit =
    match socket.connectionState with
    | Connected ws ->
        ws?send(message)
    | _ ->
        printfn "Cannot send message: WebSocket not connected"

// Close WebSocket connection
let closeSocket (socket: SocketHandle) : SocketHandle =
    match socket.connectionState with
    | Connected ws ->
        ws?close()
        { socket with connectionState = Disconnected }
    | _ -> socket

// JSON serialization helpers
let inline serializeServerMsg<'T> (msg: 'T) : string =
    Encode.Auto.toString<'T>(0, msg)

let inline deserializeClientMsg<'T> (json: string) : 'T option =
    match Decode.Auto.fromString<'T> json with
    | Result.Ok msg -> Some msg
    | Result.Error e ->
        JS.console.error("Failed to deserialize ClientMsg", json, "Error:", e)
        None

// WebSocket URL builder
let buildWebSocketUrl (baseUrl: string) : string =
    let protocol = if baseUrl.StartsWith("https") then "wss" else "ws"
    let host = baseUrl.Replace("http://", "").Replace("https://", "")
    sprintf "%s://%s/api/socket" protocol host

// Create Elmish command for WebSocket connection
let createConnectionCmd (url: string) : Cmd<SocketEvent> =
    // Create a command that will set up the WebSocket
    let connectFunc dispatch =
        try
            let onSocketEvent event = dispatch event
            let socket = createSocket url onSocketEvent
            // The ConnectionOpened event will be dispatched by the WebSocket's onopen handler
            ()
        with
        | ex -> dispatch (ConnectionError (string ex))
    
    [ connectFunc ]

// Create Elmish command for WebSocket operations  
let inline createWebSocketCmd<'TServerMessage> (socket: SocketHandle) (msg: 'TServerMessage) : Cmd<SocketEvent> =
    let json = serializeServerMsg<'TServerMessage> msg
    sendMessage socket json
    Cmd.ofMsg (MessageSent json)