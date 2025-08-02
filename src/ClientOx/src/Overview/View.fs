module Overview.View

open Fable.React
open Fable.React.Props

let root =
  div
    [ ClassName "content"; Style [ Margin "2em"] ]
    [ h1 []
        [ str "Welcome to F# Chat" ]
      h4
        [ Style [ MarginBottom "2em"] ]
        [ str "Real-time F# chat application" ] 
      p [] [ str "This is a real-time chat application built with:" ] 
      ul [] [
          li [] [b [] [str "F# + Fable"]; str " - Client-side functional programming"]
          li [] [b [] [str "Elmish"]; str " - Model-View-Update architecture"]
          li [] [b [] [str "WebSockets"]; str " - Real-time communication"]
          li [] [b [] [str "Akka.NET"]; str " - Server-side actor model"]
      ]
      p [] [ str "Select a channel from the sidebar to start chatting!" ] 
    ]