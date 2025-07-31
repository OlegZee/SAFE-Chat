module App.View

open Elmish
open Elmish.Navigation
open Elmish.UrlParser
open Fable.Core.JsInterop

open App.Types
open Router
open Chat.Types

open Fable.React
open Fable.React.Props

importAll "../sass/app.scss"

let root model dispatch =

    let mainAreaView = function
        | Route.Overview -> [Overview.View.root]
        | Channel chan ->
            let toChannelMessage m = ChannelMsg (chan, m)

            match model.chat with
            | Connected (_,chatdata) when chatdata.Channels |> Map.containsKey chan ->

                Channel.View.root chatdata.Channels.[chan]
                  (toChannelMessage >> ApplicationMsg >> ChatDataMsg >> dispatch)

            | _ ->
                [div [] [str "bad channel route" ]]

    div
      [ ClassName "container" ]
      [ div
          [ ClassName "col-md-4 fs-menu" ]
          (NavMenu.View.menu model.chat model.currentPage (ApplicationMsg >> ChatDataMsg >> dispatch))
        div
          [ ClassName "col-xs-12 col-md-8 fs-chat" ]
          (mainAreaView model.currentPage) ]

open Elmish.React
#if DEBUG
open Elmish.Debug
#endif

// App
Program.mkProgram State.init State.update root
|> Program.toNavigable (UrlParser.parseHash Router.route) State.urlUpdate
#if DEBUG
|> Program.withDebugger
#endif
|> Program.withReactSynchronous "elmish-app"
|> Program.run
