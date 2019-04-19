module UserAvatar.View

open Fable.Helpers.React
open Fable.Helpers.React.Props

open Channel.Types

let root (user: UserInfo) =
  div
    [ ClassName "fs-avatar"
      Title user.Nick
      Style <|
        match user.ImageUrl with
        | None| Some "" -> []
        | Some url -> [BackgroundImage (sprintf "url(%s)" url) ]
      ]
    []