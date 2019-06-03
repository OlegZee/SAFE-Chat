module SocketBound

open Elmish
open Browser.Dom
open Fable.Websockets.Elmish
open Fable.Websockets.Protocol

type Model<'ServerMsg,'AppModel> =
    | NotConnected
    | Connected of SocketHandle<'ServerMsg> * 'AppModel

type App<'Model,'Msg> = (unit -> 'Model * Cmd<'Msg>) * ('Msg -> 'Model -> 'Model * Cmd<'Msg>)

let inline onSockets<'AppModel,'AppMsg,'ClientMsg,'ServerMsg>
        (ainit: unit -> 'AppModel * Cmd<'AppMsg> * 'ServerMsg option)
        (aupdate: 'AppMsg -> 'AppModel -> 'AppModel * Cmd<'AppMsg> * 'ServerMsg option)
        (convertMessage: 'ClientMsg -> 'AppMsg)
        : App<Model<'ServerMsg,'AppModel>, Msg<'ServerMsg, 'ClientMsg, 'AppMsg>> =
    let init () =
        let socketAddr = sprintf "ws://%s/api/socket" document.location.host
        console.debug ("Opening socket", socketAddr)
        NotConnected, Cmd.tryOpenSocket socketAddr

    let rec update (msg: Msg<'ServerMsg, 'ClientMsg, 'AppMsg>) state =

        let respond socket: 'Model * Cmd<'Msg> * 'ServerMsg option -> _ =
            fun (newstate, cmd , serverMsg) ->
                let serverCmds = serverMsg |> Option.map (Cmd.ofSocketMessage socket) |> Option.toList
                Connected (socket, newstate), Cmd.batch <| (cmd |> Cmd.map ApplicationMsg) :: serverCmds

        match state, msg with
        | NotConnected, WebsocketMsg (socket, Opened) ->
            respond socket <| ainit ()

        | Connected (socket, appstate), ApplicationMsg msg ->
            respond socket <| aupdate msg appstate

        | Connected (socket, appstate), WebsocketMsg (_, Msg msg) ->
            respond socket <| aupdate (convertMessage msg) appstate

        | _, msg ->
            console.error ("Failed to process message", msg)
            state, Cmd.none
    init, update

// let ainit, aupdate =
//     onSockets
//         (fun () ->
//             let model, cmd = ChatServer.State.init0 ()
//             model, cmd, Some FsChat.Protocol.ServerMsg.Greets )
//         ChatServer.State.update
//         ChatServer.Types.ServerMessage
