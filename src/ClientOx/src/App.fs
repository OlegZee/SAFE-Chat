module App.View

open Elmish
open Elmish.Navigation
open Elmish.UrlParser
open Fable.Core.JsInterop

open App.Types
open FsChat
open Router
open Chat.Types

open Fable.React
open Fable.React.Props

importAll "../sass/app.scss"

module ExploreThoth =
    let message = """["Hello",{"me":{"id":"127","nick":"olegz3","isbot":false,"status": null,"email":"","imageUrl":"https://www.gravatar.com/avatar/d19d94188a4a8c6ac37c9e022e8320df?d=monsterid"},"channels":[{"id":"104","name":"About","userCount":0,"topic":"interactive help"},{"id":"103","name":"Demo","userCount":0,"topic":"Channel for testing purposes. Notice the bots are always ready to keep conversation."},{"id":"102","name":"Test","userCount":0,"topic":"empty channel"}]}]"""
    let decoded = Thoth.Json.Decode.Auto.fromString<FsChat.Protocol.ClientMsg> message
    Fable.Core.JS.console.log("Decoded message: ", decoded)
    
    let clientMsg: FsChat.Protocol.ClientMsg =
        Protocol.Hello {
          me = { id = "126"; nick = "olegz1"; isbot = false; status = null; email = ""; imageUrl = "https://www.gravatar.com/avatar/639145462a7e66733e20cd65b222196a?d=monsterid" }
          channels = [ { id = "104"; name = "About"; userCount = 0; topic = "interactive help" }
                       { id = "103"; name = "Demo"; userCount = 0; topic = "Channel for testing purposes. Notice the bots are always ready to keep conversation." }
                       { id = "102"; name = "Test"; userCount = 0; topic = "empty channel" } ] }
    let encoded = Thoth.Json.Encode.Auto.toString(0, clientMsg, skipNullField = false)
    Fable.Core.JS.console.log("***** Encoded message: ", encoded)

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
open Elmish.HMR
#endif

// App
Program.mkProgram State.init State.update root
|> Program.toNavigable (UrlParser.parseHash Router.route) State.urlUpdate
#if DEBUG
|> Program.withConsoleTrace
#endif
#if DEBUG
|> Elmish.HMR.Program.withReactSynchronous "elmish-app"
#else
|> Program.withReactSynchronous "elmish-app"
#endif
|> Program.run
