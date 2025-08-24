module UserAvatar.View

open Fable.React
open Fable.React.Props

let root  =
  function
  | None | Some "" ->
      div [ ClassName "fs-avatar" ] 
          [ i [ ClassName "mdi mdi-account" ] [] ]

  | Some url ->
      div
          [ ClassName "fs-avatar"
            Style [BackgroundImage (sprintf "url(%s)" url) ] ]
          []
