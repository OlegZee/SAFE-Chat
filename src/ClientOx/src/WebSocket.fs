module WebSocket

open Fable.Core
open Fable.Core.JsInterop
open Elmish
open Thoth.Json

open FsChat

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
    | ConnectionOpened
    | ConnectionClosed
    | ConnectionError of string
    | MessageReceived of string
    | MessageSent of string

// Create WebSocket connection
let createSocket (url: string) (onEvent: SocketEvent -> unit) : SocketHandle =
    let ws: obj = createNew "WebSocket" [| url |]
    
    // Set up event handlers using dynamic property assignment
    ws?onopen <- (fun _ -> onEvent ConnectionOpened)
    ws?onclose <- (fun _ -> onEvent ConnectionClosed)
    ws?onerror <- (fun _ -> onEvent (ConnectionError "WebSocket error"))
    ws?onmessage <- (fun event -> onEvent (MessageReceived event?data))
    
    {
        connectionState = Connected ws
        url = url
    }

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
let serializeServerMsg (msg: Protocol.ServerMsg) : string =
    Encode.Auto.toString(0, msg)

let deserializeClientMsg (json: string) : Protocol.ClientMsg option =
    // Temporary implementation for testing
    None

// WebSocket URL builder
let buildWebSocketUrl (baseUrl: string) : string =
    let protocol = if baseUrl.StartsWith("https") then "wss" else "ws"
    let host = baseUrl.Replace("http://", "").Replace("https://", "")
    sprintf "%s://%s/api/socket" protocol host

// Create Elmish command for WebSocket operations
let createWebSocketCmd (socket: SocketHandle) (msg: Protocol.ServerMsg) : Cmd<SocketEvent> =
    let json = serializeServerMsg msg
    sendMessage socket json
    Cmd.ofMsg (MessageSent json)