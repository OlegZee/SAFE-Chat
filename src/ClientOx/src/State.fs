module State

open Elmish
open Elmish.Navigation
open Fable.Core.JsInterop
open Router
open App.Types

let private jsConsole = Fable.Core.JS.console

let urlUpdate (result: Option<Route>) model =
    match result with
    | None ->
        // console.error("Error parsing url")
        model, Navigation.modifyUrl  "#" // no matching route - go home
        // model,Navigation.modifyUrl (toHash model.currentPage)
    | Some route ->
        { model with currentPage = route }, []

let init result =
    let (chinfo, chinfoCmd) = Chat.State.init()
    let (model, cmd) = urlUpdate result { currentPage = Overview; chat = chinfo }
    // Initialize WebSocket connection
    let wsUrl = "ws://localhost:8083/api/socket"
    jsConsole?log("Connecting to WebSocket at %s", wsUrl)
    let connectCmd = Cmd.ofMsg (ChatDataMsg (Chat.Types.ApplicationMsg (Chat.Types.ConnectWebSocket wsUrl)))
    model, Cmd.batch [ cmd
                       Cmd.map (ChatDataMsg) chinfoCmd
                       connectCmd
                       ]

let update msg model =
    match msg with
    | ChatDataMsg msg ->
        let (chinfo, chinfoCmd) = Chat.State.update msg model.chat
        { model with chat = chinfo }, Cmd.map ChatDataMsg chinfoCmd
