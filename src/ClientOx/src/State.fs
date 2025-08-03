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
    let (chinfo, chinfoCmd) = ChatPage.State.init()
    let (model, cmd) = urlUpdate result { currentPage = Overview; chat = chinfo }
    model, Cmd.batch [ cmd
                       Cmd.map (ChatDataMsg) chinfoCmd
                       ]

let update msg model =
    match msg with
    | ChatDataMsg msg ->
        let (chinfo, chinfoCmd) = ChatPage.State.update msg model.chat
        { model with chat = chinfo }, Cmd.map ChatDataMsg chinfoCmd
