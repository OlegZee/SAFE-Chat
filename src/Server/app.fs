module App

open System.Net
open System.IO
open FSharp.Control.Tasks.ContextInsensitive

open Giraffe
open GiraffeViewEngine
open Microsoft.Extensions.Logging
open Microsoft.AspNetCore.Http
// open Suave
// open Suave.OAuth
// open Suave.Authentication
// open Suave.Operators
// open Suave.Logging
// open Suave.Filters
// open Suave.Redirection
// open Suave.Successful
// open Suave.RequestErrors
// open Suave.State.CookieStateStore

open Akka.Configuration
open Akka.Actor
open Akkling
open Akkling.Streams

open ChatTypes
open ChatUser
open UserStore
open ChatServer
// open Suave.Html
open UserSessionFlow

// ---------------------------------
// Web app
// ---------------------------------
let private (</>) a b = Path.Combine(a, b)

module Secrets =

    open Microsoft.Extensions.Configuration
    open System.Security.Cryptography
    
    let CookieSecretFile = "CHAT_DATA" </> "COOKIE_SECRET"
    let OAuthConfigFile = "CHAT_DATA" </> "suave.oauth.config"
    let KeyLength = 128/8

    let readCookieSecret () =
        printfn "Reading configuration data from %s" System.Environment.CurrentDirectory
        if not (File.Exists CookieSecretFile) then
            let cryptRandom = RandomNumberGenerator.Create()
            let secret =
                let bytes = Array.zeroCreate<byte> (int KeyLength)
                do cryptRandom.GetBytes bytes
                bytes

            do (Path.GetDirectoryName CookieSecretFile) |> Directory.CreateDirectory |> ignore
            File.WriteAllBytes (CookieSecretFile, secret)

        File.ReadAllBytes(CookieSecretFile)

    // Here I'm reading my API keys from file stored in my CHAT_DATA/suave.oauth.config folder
    let private oauthConfigData =
        if not (File.Exists OAuthConfigFile) then
            do (Path.GetDirectoryName OAuthConfigFile) |> Directory.CreateDirectory |> ignore
            File.WriteAllText (OAuthConfigFile, """{
      "google": {
      	"client_id": "<type in client id string>",
      	"client_secret": "<type in client secret>"
      	},
}"""    )

        ConfigurationBuilder().SetBasePath(System.Environment.CurrentDirectory) .AddJsonFile(OAuthConfigFile).Build()

    // let dump name a =
    //     printfn "%s: %A" name a
    //     a

    // let oauthConfigs =
    //     defineProviderConfigs (fun pname c ->
    //         let key = pname.ToLowerInvariant()
    //         {c with
    //             client_id = oauthConfigData.[key + ":client_id"]
    //             client_secret = oauthConfigData.[key + ":client_secret"]}
    //     )
    //     // |> dump "oauth configs"

type ServerActor = IActorRef<ChatServer.ServerCommand>
let mutable private appServerState = None

let startChatServer (loggerFactory: ILoggerFactory) = async {
    let inline replace (str: string, sub: string) : string -> string = function s -> s.Replace(str, sub) 
    let journalFileName: string = "CHAT_DATA" </> "journal.db"
    let configStr = """akka {  
    stdout-loglevel = WARNING
    loglevel = DEBUG
    persistence {
        journal {
            plugin = "akka.persistence.journal.sqlite"
            sqlite {
                class = "Akka.Persistence.Sqlite.Journal.SqliteJournal, Akka.Persistence.Sqlite"
                connection-string = "Data Source=$JOURNAL$;cache=shared;"
                connection-timeout = 30s
                auto-initialize = on

                event-adapters {
                  json-adapter = "AkkaStuff+EventAdapter, fschathost"
                }            
                event-adapter-bindings {
                  # to journal
                  "System.Object, mscorlib" = json-adapter
                  # from journal
                  "Newtonsoft.Json.Linq.JObject, Newtonsoft.Json" = [json-adapter]
                }
            }
        }
    }
    actor {
        ask-timeout = 2000
        debug {
            # receive = on
            # autoreceive = on
            # lifecycle = on
            # event-stream = on
            unhandled = on
        }
    }
}"""
    let config = configStr |> replace ("$JOURNAL$", replace ("\\", "\\\\") journalFileName) |> ConfigurationFactory.ParseString

    let actorSystem = ActorSystem.Create("chatapp", config)
    let userStore = UserStore (actorSystem, loggerFactory.CreateLogger "userstore")

    do! Async.Sleep(1000)

    let logas = loggerFactory.CreateLogger
    let chatServer = startServer actorSystem (logas "server")
    do! Diag.createDiagChannel userStore.GetUser (logas "diagchan") actorSystem chatServer (UserStore.UserIds.echo, "Demo", "Channel for testing purposes. Notice the bots are always ready to keep conversation.")

    do! chatServer |> getOrCreateChannel "Test" "empty channel" (GroupChatChannel { autoRemove = false }) |> Async.Ignore
    do! chatServer |> getOrCreateChannel "About" "interactive help" (OtherChannel <| AboutChannelActor.props UserIds.system (logas "aboutuser")) |> Async.Ignore

    appServerState <- Some (actorSystem, userStore, chatServer)
    return ()
}

let returnPathOrHome: HttpHandler = 
    handleContext(fun ctx ->
        match ctx.TryGetQueryStringValue "returnPath" with
        | Some path -> path
        | _ -> "/"
        |> ctx.WriteTextAsync)
    >=> setStatusCode StatusCodes.Status302Found

// let sessionStore setF = handleContext (fun x ->
//     match HttpContext.state x with
//     | Some state -> setF state
//     | None -> never)

// let session (userStore: UserStore) (f: ClientSession -> HttpHandler) = 
//     statefulForSession
//     >=> context (HttpContext.state >>
//         function
//         | None -> f NoSession
//         | Some state ->
//             match state.get "userid" with
//             | Some userid ->
//                 fun ctx -> async {
//                     let! result = userStore.GetUser (UserId userid)
//                     match result with
//                     | Some me ->
//                         return! f (UserLoggedOn (RegisteredUser (UserId userid, me))) ctx
//                     | None ->
//                         logger.error (Message.eventX "Failed to get user from user store {id}" >> Message.setField "id" userid)
//                         return! f NoSession ctx
//                 }
                
//             | _ -> f NoSession)

// let getUserImageUrl (claims: Map<string,obj>) : string option =
//     let getClaim claim () = claims |> Map.tryFind claim |> Option.map string

//     None
//     |> Option.orElseWith (getClaim "avatar_url")
//     |> Option.orElseWith (getClaim "picture")

let private noCache =
    responseCaching NoCache None None

let private found location : HttpHandler =
    setStatusCode 302 >=> setHttpHeader "Location" (location: string)

let private sessionHandler (userStore: UserStore) (session) : HttpHandler =
    choose [
        // GET >=> route "/" >=> noCache >=> (
        //     match session with
        //     | NoSession -> found "/logon"
        //     | _ -> Files.browseFileHome "index.html"
        //     )
        // handlers for login form
        route "/logon" >=> choose [
            GET >=> noCache >=>
                (Logon.Views.index session |> renderHtmlNode |> Successful.OK)
            POST >=> (
                fun next ctx -> task {
                    let! body = ctx.ReadBodyFromRequestAsync()
                    let nick = body.Substring 5 |> WebUtility.UrlDecode  |> WebUtility.HtmlDecode
                    let user: ChatUser.UserInfo = { makeNew (Anonymous nick) nick with imageUrl = makeUserImageUrl "monsterid" nick}
                    let! registerResult = userStore.Register user
                    match registerResult with
                    | Ok (RegisteredUser(UserId userid, _)) ->
                        let logger = ctx.GetLogger("logon")
                        logger.LogInformation ("Anonymous login by nick {0}", nick)

                        // TODO
                        // return! (statefulForSession
                        //     >=> sessionStore (fun store -> store.set "userid" userid)
                        //     >=> FOUND "/") ctx
                        return! found "/" next ctx
                    | Result.Error message ->
                        return! (Successful.OK <| sprintf "Register failed because of `%s`" message) next ctx
                }
            )
        ]
        GET >=> route "/logoff" >=> noCache >=>
            found "/logon"
            // TODO
            // deauthenticate >=> (warbler(fun _ ->
            //     match session with
            //     | UserLoggedOn user ->
            //         logger.info (Message.eventX "LOGOFF: Unregistering {nick}"
            //             >> Message.setFieldValue "nick" (getUserNick user))
            //         do userStore.Unregister (getUserId user)
            //     | _ -> ()
            //     FOUND "/logon"
            // ))
        // path "/api/socket" >=>
        //     match session with
        //     | UserLoggedOn user -> fun ctx -> async {
        //         let session = UserSession.Session(server, userStore, user)
        //         let materializer = actorSystem.Materializer()

        //         let messageFlow = createMessageFlow materializer
        //         let socketFlow = createSessionFlow userStore messageFlow session.ControlFlow

        //         let materialize materializer source sink =
        //             session.SetListenChannel(
        //                 source
        //                 |> Source.viaMat socketFlow Keep.right
        //                 |> Source.toMat sink Keep.left
        //                 |> Graph.run materializer |> Some)
        //             ()

        //         logger.debug (Message.eventX "Opening socket for {user}" >> Message.setField "user" (getUserNick user))
        //         let! result = WebSocket.handShake (SocketFlow.handleWebsocketMessages actorSystem materialize) ctx
        //         logger.debug (Message.eventX "Closing socket for {user}" >> Message.setField "user" (getUserNick user))

        //         return result
        //         }
        //     | NoSession ->
        //         BAD_REQUEST "Authorization required"

        // Files.browseHome
    ]


let root: HttpHandler =
        match appServerState with
        | None ->
            ServerErrors.SERVICE_UNAVAILABLE "Server is not started"
        | Some (actorSystem, userStore, server) ->
            choose [
                RequestErrors.NOT_FOUND "Not Found"
//    fun (next : HttpFunc) (ctx : HttpContext) ->
//                 warbler(fun ctx ->
//                     // problem is that redirection leads to localhost and authorization does not go well
//                     let authorizeRedirectUri =
//                         (ctx.runtime.matchedBinding.uri "oalogin" "").ToString().Replace("127.0.0.1", "localhost")

//                     authorize authorizeRedirectUri Secrets.oauthConfigs
//                         (fun loginData ctx -> async {
//                             let imageUrl =
//                                 getUserImageUrl loginData.ProviderData
//                                 |> Option.orElseWith (fun () -> makeUserImageUrl "wavatar" loginData.Name)

//                             let identity = Person {oauthId = Some loginData.Id; email = None; name = None}
//                             let user = {ChatUser.makeNew identity loginData.Name with imageUrl = imageUrl}

//                             let! registerResult = userStore.Register user
//                             match registerResult with
//                             | Ok (RegisteredUser(UserId userid, _)) ->
                                
//                                 logger.info (Message.eventX "User registered via oauth \"{name}\""
//                                     >> Message.setFieldValue "name" loginData.Name)

//                                 return! (statefulForSession
//                                     >=> sessionStore (fun store -> store.set "userid" userid)
//                                     >=> FOUND "/") ctx
//                             | Result.Error message ->
//                                 return! (OK <| sprintf "Register failed because of `%s`" message) ctx
//                             }
//                         )
//                         (fun () -> FOUND "/logon")
//                         (fun error -> OK <| sprintf "Authorization failed because of `%s`" error.Message)
//                     )
            ]
