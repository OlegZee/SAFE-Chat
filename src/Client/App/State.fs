module App.State

open Elmish
open Elmish.Navigation
open Router
open Types

open SocketBound

let appinit, appupdate =
    onSockets
        (fun () ->
            let model, cmd = ChatServer.State.init0 ()
            model, cmd, Some FsChat.Protocol.ServerMsg.Greets )
        ChatServer.State.update
        ChatServer.Types.ServerMessage

let urlUpdate (result: Option<Route>) model =
    match result with
    | None ->
        // console.error("Error parsing url")
        model, Navigation.modifyUrl  "#" // no matching route - go home
        // model,Navigation.modifyUrl (toHash model.currentPage)
    | Some route -> 
        { model with currentPage = route }, []

let init result =
    let (chinfo, chinfoCmd) = appinit()
    let (model, cmd) = urlUpdate result { currentPage = Overview; chat = chinfo }
    model, Cmd.batch [ cmd
                       Cmd.map (ChatDataMsg) chinfoCmd
                       ]

let update msg model =
    match msg with
    | ChatDataMsg msg ->
        let (chinfo, chinfoCmd) = appupdate msg model.chat
        { model with chat = chinfo }, Cmd.map ChatDataMsg chinfoCmd
