module App.State

open Elmish
open Elmish.Navigation
open Router
open Types
open Browser.Dom
open Browser.WebStorage

let private setThemeClass theme =
    let className = "theme-" + theme
    document.documentElement.className <- className

let private loadThemeFromStorage () =
    try
        localStorage.getItem("selected-theme")
        |> Option.ofObj
        |> Option.defaultValue "mass-effect"
    with
    | _ -> "mass-effect"

let private saveThemeToStorage theme =
    try
        localStorage.setItem("selected-theme", theme)
    with
    | _ -> ()

let urlUpdate (result: Option<Route>) model =
    match result with
    | None ->
        // console.error("Error parsing url")
        { model with currentPage = Overview }, Navigation.modifyUrl "#"
    | Some route ->
        { model with currentPage = route }, []

let init result =
    let connModel, connCmd = Connection.State.init()
    let savedTheme = loadThemeFromStorage()
    setThemeClass savedTheme
    let model, cmd = urlUpdate result { currentPage = Overview; chatPage = connModel; selectedTheme = savedTheme }
    model, Cmd.batch [
        cmd
        Cmd.map ChatDataMsg connCmd
    ]

let update msg model =
    match msg with
    | ChatDataMsg msg ->
        let (chinfo, chinfoCmd) = Connection.State.update msg model.chatPage
        { model with chatPage = chinfo }, Cmd.map ChatDataMsg chinfoCmd
    | SetTheme theme ->
        setThemeClass theme
        saveThemeToStorage theme
        { model with selectedTheme = theme }, Cmd.none