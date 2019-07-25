module Logon

open Giraffe.GiraffeViewEngine

open ChatUser

type ClientSession = NoSession | UserLoggedOn of RegisteredUser

module Views =
    let private partUser (session : ClientSession) = 
        div [ _id "part-user"] [
            match session with
            | UserLoggedOn (RegisteredUser (_, user)) ->
                yield p [] [Text (sprintf "Logged on as %s" user.nick)]
                yield p [][]
                yield a [_href "/"] [Text "Proceed to chat screen"]
                yield p [][]
                yield Text "Or you can "
                yield a [_href "/logoff"] [Text "log off now"]
                yield p [][]
            | _ ->
                yield tag "form" [_method "POST"] (
                    [ p [_class "subtitle"]
                        [ Text "Log on using your "
                          a [_href "/oaquery?provider=Google"] [Text "Google"]
                          Text " or "
                          a [_href "/oaquery?provider=Github"] [Text "Github"]
                          Text " account, or..." ]
                      div [_class "label"]
                        [ Text "Choose a nickname" ]
                      div [_class "field"]
                        [ div [_class "control"]
                            [ tag "input" [_id "nickname"; _class "input"; _name "nick"; _type "text"; _required] [] ]]
                      div [_class "control"]
                          [ tag "input" [
                              _id "login"
                              _class "button is-primary"
                              _type "submit"
                              _value "Connect anonymously"] [] ] ]
                )
        ]

    let page content =
        html []
            [ head []
                [ title [] [Text "F# Chat server"]
                  link [ _rel "stylesheet"
                         _href "https://cdnjs.cloudflare.com/ajax/libs/bulma/0.6.1/css/bulma.css" ]
                  link [ _rel "stylesheet"
                         _href "logon.css" ] ]
              body []
                [ div [_id "header"]
                    [ tag "h1" [_class "title"] [Text "F# Chat server"]
                      tag "h1" [_class "subtitle"] [Text "Logon screen"]
                      hr [] ]
                  content

                  tag "footer" [_class "footer"]
                    [ div [ _class "container"]
                        [ div [ _class "content has-text-centered"]
                            [   tag "strong" [] [Text "F# Chat"]
                                Text " built by "
                                a [_href "https://github.com/OlegZee"] [Text "Anonymous"]
                                Text " with (in alphabetical order) "
                                a [_href "http://getakka.net"] [Text "Akka.NET"]
                                Text ", "
                                a [_href "https://github.com/Horusiath/Akkling"] [Text "Akkling"]
                                Text ", "
                                a [_href "http://fable.io"] [Text "Fable"]
                                Text ", "
                                a [_href "http://ionide.io"] [Text "Ionide"]
                                Text " and "
                                a [_href "http://suave.io"] [Text "Suave.IO"] ]
                        ]]
                ]]

    let index session = page (partUser session)
