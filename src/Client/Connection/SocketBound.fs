module SocketBound

open Elmish
open Browser.Dom
open Fable.Websockets.Elmish
open Fable.Websockets.Protocol

type Model<'ServerMsg,'AppModel> =
    | NotConnected
    | Connecting
    | Connected of SocketHandle<'ServerMsg> * 'AppModel


type App<'Model,'Msg> = {
    init: unit -> 'Model * Cmd<'Msg>
    update: 'Msg -> 'Model -> 'Model * Cmd<'Msg>
}

type ConnectedApp<'Model,'Msg,'ClientMsg,'ServerMsg, 'AppMsg> = {
    init: 'ClientMsg -> Result<'Model * Cmd<'Msg>, string>
    update: 'Msg -> 'Model -> 'Model * Cmd<'Msg> * 'ServerMsg option
    greets: 'ServerMsg
    convertMessage: 'ClientMsg -> 'AppMsg
}

let onSockets<'AppModel,'AppMsg,'ClientMsg,'ServerMsg>
        (app: ConnectedApp<'AppModel,'AppMsg, 'ClientMsg, 'ServerMsg, 'AppMsg>)
        : App<Model<'ServerMsg,'AppModel>, Msg<'ServerMsg, 'ClientMsg, 'AppMsg>> =
    let init () =
        let socketAddr = sprintf "ws://%s/api/socket" document.location.host
        console.debug ("Opening socket", socketAddr)
        NotConnected, Cmd.tryOpenSocket socketAddr

    let rec update (msg: Msg<'ServerMsg, 'ClientMsg, 'AppMsg>) state = 

        match state, msg with
        | NotConnected, WebsocketMsg (socket, Opened) ->
            Connecting, Cmd.ofSocketMessage socket app.greets

        | Connecting, WebsocketMsg (socket, Msg msg) ->
            match app.init msg with
            | Ok (serverData, cmd) ->
                Connected (socket, serverData), cmd |> Cmd.map ApplicationMsg
            | Result.Error errorMessage ->
                console.error ("Failed to process message while connecting", errorMessage)
                state, Cmd.none

        | Connected _, WebsocketMsg (_, Msg msg) ->
            update (app.convertMessage msg |> ApplicationMsg) state

        | _, msg ->
            console.error ("Failed to process message", msg)
            state, Cmd.none
    {
        init = init
        update = update
    }