module AppOx

open System.Net
open System.IO

// Oxpecker imports - modern ASP.NET Core based framework
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Oxpecker

open Akka.Configuration
open Akka.Actor
open Akkling
open Akkling.Streams

open ChatTypes
open ChatUser
open UserStore
open ChatServer
open Logon
open UserSessionFlow

// ---------------------------------
// Configuration and State
// ---------------------------------

type AppState = {
    ActorSystem: ActorSystem option
    UserStore: UserStore.UserStoreT option  
    ChatServer: ChatServer.ServerT option
}

let mutable appServerState: AppState = { ActorSystem = None; UserStore = None; ChatServer = None }

// ---------------------------------
// WebApp Configuration
// ---------------------------------

let configureServices (services: IServiceCollection) =
    services
        .AddRouting()
        .AddOxpecker()
    |> ignore

let webApp : RequestDelegate =
    choose [
        route "/" >=> text "F# Chat - Oxpecker Version"
        route "/health" >=> text "OK"
        route "/api/test" >=> json {| message = "Hello from Oxpecker!" |}
    ]

// ---------------------------------
// Server Startup
// ---------------------------------

let startChatServer() = async {
    let dataPath = 
        match appServerState.ActorSystem with
        | Some _ -> ""
        | None -> "CHAT_DATA"
    
    do Directory.CreateDirectory dataPath |> ignore
    let journalFileName = Path.Combine(dataPath, "journal.db")
    
    let configStr = $"""
    akka {{
        loglevel = INFO
        
        persistence {{
            journal {{
                plugin = "akka.persistence.journal.sqlite"
                sqlite {{
                    class = "Akka.Persistence.Sqlite.Journal.SqliteJournal, Akka.Persistence.Sqlite"
                    connection-string = "Data Source={journalFileName.Replace("\\", "\\\\")};cache=shared;"
                    connection-timeout = 30s
                    auto-initialize = on
                }}
            }}
        }}
        
        actor {{
            ask-timeout = 2000
            serializers {{
                json = "Akka.Serialization.NewtonSoftJsonSerializer"
            }}
            serialization-bindings {{
                "System.Object" = json
            }}
            debug {{
                unhandled = on
            }}
        }}
    }}"""
    
    let config = ConfigurationFactory.ParseString(configStr)
    let actorSystem = ActorSystem.Create("chatapp", config)
    let userStore = UserStore.startUserStore actorSystem
    let server = ChatServer.startServer actorSystem
    
    appServerState <- { 
        ActorSystem = Some actorSystem
        UserStore = Some userStore 
        ChatServer = Some server 
    }
    
    printfn "Chat server started successfully"
}

let configureApp (app: IApplicationBuilder) =
    app
        .UseRouting()
        .UseOxpecker(webApp)
    |> ignore

// ---------------------------------
// Web Host
// ---------------------------------

let createWebHost() =
    WebApplication.CreateBuilder()
        .ConfigureServices(configureServices)
        .Build()
        .ConfigureWith(configureApp)