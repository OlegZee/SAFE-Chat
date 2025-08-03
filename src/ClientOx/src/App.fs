module App.View

open Elmish
open Elmish.Navigation
open Fable.Core.JsInterop

open App.Types
open Router
open ChatPage.Types

open Fable.React
open Fable.React.Props

importAll "../sass/app.scss"

let root model dispatch =

    let mainAreaView = function
        | Route.Overview -> [OverviewPage.View.root]
        | Channel chan ->
            let toChannelMessage m = ChannelMsg (chan, m)

            match model.chat with
            | Connected (_,chatdata) when chatdata.ConnectedChannels |> Map.containsKey chan ->

                Channel.View.root chatdata.ConnectedChannels.[chan]
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

// App
Program.mkProgram State.init State.update root
|> Program.toNavigable (UrlParser.parseHash Router.route) State.urlUpdate
#if DEBUG
|> Program.withConsoleTrace
|> HMR.Program.withReactSynchronous "elmish-app"
#else
|> Program.withReactSynchronous "elmish-app"
#endif
|> Program.run
