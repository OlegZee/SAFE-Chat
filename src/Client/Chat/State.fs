module Chat.State

open Browser.Dom
open Elmish

open Fable.Websockets.Elmish
open Fable.Websockets.Protocol

open Chat.Types
open FsChat

let init () : ChatState * Cmd<Msg> =
    let socketAddr = sprintf "ws://%s/api/socket" document.location.host
    console.debug ("Opening socket", socketAddr)
    NotConnected, Cmd.tryOpenSocket socketAddr

let update msg state : ChatState * Cmd<Msg> = 
    match msg with
    | ApplicationMsg amsg ->
        match state with
        | Connected chat ->
            let newServerModel, cmd, serverMsg = RemoteServer.State.update amsg chat.serverData
            let commands =
                [ Cmd.map ApplicationMsg cmd ]
                @ (serverMsg |> Option.map (Cmd.ofSocketMessage chat.socket) |> Option.toList)

            Connected { chat with serverData = newServerModel }, Cmd.batch commands
        | _ ->
            console.error "Failed to process channel message. Server is not connected"
            state, Cmd.none

    | WebsocketMsg (socket, Opened) ->
        let remoteServerState, _ =  RemoteServer.State.init()   // TODO pass second parameter
        Connected { serverData = remoteServerState; socket = socket }, Cmd.ofSocketMessage socket Protocol.ServerMsg.Greets

    | WebsocketMsg (_, Msg msg) ->
        match state with
        | Connected ({ serverData = serverData } as chat) ->
            let newServerData, cmd = RemoteServer.State.chatUpdate msg serverData
            Connected { chat with serverData = newServerData }, cmd |> Cmd.map ApplicationMsg
        | other ->
            console.info (sprintf "Socket message %A" other)
            (other, Cmd.none)

    | _ -> state, Cmd.none
