module Chat.State

open Elmish
open Fable.Core.JsInterop

let private jsConsole: obj = emitJsStatement () "console"

open Router
open WebSocket

open Channel.Types
open Chat.Types

open FsChat

module private Conversions =

    let mapUserInfo isMe (u: Protocol.ChanUserInfo) :UserInfo =
        { Id = u.id; Nick = u.nick; IsBot = u.isbot
          Status = u.status
          Online = true; ImageUrl = Core.Option.ofObj u.imageUrl
          isMe = isMe u.id}

    let mapChannel (ch: Protocol.ChannelInfo) : ChannelInfo =
        {Id = ch.id; Name = ch.name; Topic = ch.topic; UserCount = ch.userCount}

    let mapUserMessage (msg: Protocol.ChannelMessageInfo) = (msg.author, {Id = msg.id; Ts = msg.ts; Content = msg.text})

module private Implementation =

    let updateChanCmd chanId (f: ChannelData -> ChannelData * Channel.Types.Msg Cmd) (chat: ChatData) : ChatData * Types.Msg Cmd =
        match chat.Channels |> Map.tryFind chanId with
        | Some channel ->
            let newData, cmd = f channel
            { chat with Channels = chat.Channels |> Map.add chanId newData },
              cmd |> Cmd.map (fun x -> Types.ApplicationMsg (ChannelMsg (chanId, x)))
        | None ->
            jsConsole?error ("Channel %s update failed - channel not found", chanId)
            chat, Cmd.none

    let updateChan chanId (f: ChannelData -> ChannelData) (chat: ChatData) : ChatData =
        match chat.Channels |> Map.tryFind chanId with
        | Some channel ->
            match f channel with
            | newData when newData = channel -> chat
            | newData -> { chat with Channels = chat.Channels |> Map.add chanId newData }
        | None ->
            jsConsole?error ("Channel %s update failed - channel not found", chanId)
            chat

    let mutable lastRequestId = 10000
    let toCommand x =
        let reqId = lastRequestId.ToString()
        lastRequestId <- lastRequestId + 1
        Protocol.ServerMsg.ServerCommand (reqId, x)

    let applicationMsgUpdate (msg: AppMsg) (state: ChatData) :(ChatData * Types.Msg Cmd) =

        match msg with
        | Nop -> state, Cmd.none

        | ChannelMsg (chanId, Forward text) ->
            let message = Protocol.UserMessage {text = text; chan = chanId}
            let cmd = WebSocket.createWebSocketCmd state.socket message |> Cmd.map SocketEvent
            state, cmd

        | ChannelMsg (chanId, Msg.Leave) ->
            let command = Protocol.Leave chanId |> toCommand
            let cmd = WebSocket.createWebSocketCmd state.socket command |> Cmd.map SocketEvent
            state, cmd

        | ChannelMsg (chanId, msg) ->
            let newState, cmd = state |> updateChanCmd chanId (Channel.State.update msg)
            newState, cmd

        | SetNewChanName name ->
            { state with NewChanName = name }, Cmd.none
            
        | CreateJoin ->
            match state.NewChanName with
            | Some channelName ->
                let command = Protocol.JoinOrCreate channelName |> toCommand
                let cmd = WebSocket.createWebSocketCmd state.socket command |> Cmd.map SocketEvent
                { state with NewChanName = None }, cmd
            | None -> state, Cmd.none

        | Join chanId ->
            let command = Protocol.Join chanId |> toCommand
            let cmd = WebSocket.createWebSocketCmd state.socket command |> Cmd.map SocketEvent
            state, cmd
            
        | Leave chanId ->
            let command = Protocol.Leave chanId |> toCommand
            let cmd = WebSocket.createWebSocketCmd state.socket command |> Cmd.map SocketEvent
            state, cmd
            
        | ConnectWebSocket url ->
            let newSocket = WebSocket.createSocket url (fun event -> 
                jsConsole?log ("WebSocket event:", event)
                // This would be handled by the Elmish dispatch mechanism
            )
            { state with socket = newSocket }, Cmd.none
            
        | DisconnectWebSocket ->
            let closedSocket = WebSocket.closeSocket state.socket
            { state with socket = closedSocket }, Cmd.none

    // Handle incoming WebSocket messages
    let handleSocketEvent (event: WebSocket.SocketEvent) (state: ChatData) : ChatData * Types.Msg Cmd =
        match event with
        | WebSocket.ConnectionOpened ->
            jsConsole?log "WebSocket connected, sending Greets"
            let cmd = WebSocket.createWebSocketCmd state.socket Protocol.Greets |> Cmd.map SocketEvent
            state, cmd
            
        | WebSocket.ConnectionClosed ->
            jsConsole?log "WebSocket disconnected"
            let disconnectedSocket = { state.socket with connectionState = Disconnected }
            { state with socket = disconnectedSocket }, Cmd.none
            
        | WebSocket.ConnectionError error ->
            jsConsole?log ("WebSocket error:", error)
            let errorSocket = { state.socket with connectionState = Error error }
            { state with socket = errorSocket }, Cmd.none
            
        | WebSocket.MessageReceived json ->
            match WebSocket.deserializeClientMsg json with
            | Some clientMsg ->
                jsConsole?log ("Received client message:", clientMsg)
                state, Cmd.ofMsg (SendClientMsg clientMsg)
            | None ->
                jsConsole?log ("Failed to parse message:", json)
                state, Cmd.none
                
        | WebSocket.MessageSent json ->
            jsConsole?log ("Sent message:", json)
            state, Cmd.none

    // Handle incoming server messages (converted from ClientMsg)
    let handleServerMessage (msg: Protocol.ClientMsg) (state: ChatData) : ChatData * Types.Msg Cmd =
        match msg with
        | Protocol.Hello helloInfo ->
            jsConsole?log ("Hello received:", helloInfo)
            // Update channel list and user info
            let channels = helloInfo.channels |> List.map (fun ch -> ch.id, Conversions.mapChannel ch) |> Map.ofList
            { state with ChannelList = channels }, Cmd.none
            
        | Protocol.CmdResponse (reqId, response) ->
            jsConsole?log ("Command response:", reqId, response)
            // Handle command responses (join, leave, etc.)
            state, Cmd.none
            
        | Protocol.ChanMsg msgInfo ->
            jsConsole?log ("Channel message:", msgInfo)
            // Update channel with new message
            let (authorId, message) = Conversions.mapUserMessage msgInfo
            state, Cmd.none
            
        | Protocol.ServerEvent eventInfo ->
            jsConsole?log ("Server event:", eventInfo)
            // Handle server events (user joined, left, etc.)
            state, Cmd.none

let init () : ChatState * Types.Msg Cmd =
    NotConnected, Cmd.none

let update (msg : Types.Msg) (state : ChatState) : ChatState * Types.Msg Cmd =
    match state, msg with
    | NotConnected, _ ->
        // TODO: Implement authentication and connection logic
        jsConsole?log ("Not connected, ignoring message:", msg)
        state, Cmd.none
        
    | Connected (user, chat), ApplicationMsg appMsg ->
        let newChat, cmd = Implementation.applicationMsgUpdate appMsg chat
        Connected (user, newChat), cmd
        
    | Connected (user, chat), SendClientMsg clientMsg ->
        let newChat, cmd = Implementation.handleServerMessage clientMsg chat
        Connected (user, newChat), cmd
        
    | Connected (user, chat), SocketEvent socketEvent ->
        let newChat, cmd = Implementation.handleSocketEvent socketEvent chat
        Connected (user, newChat), cmd
        
    | NotConnected, ApplicationMsg (ConnectWebSocket url) ->
        // Allow connection from NotConnected state
        let initialChat = ChatData.Empty
        let newChat, cmd = Implementation.applicationMsgUpdate (ConnectWebSocket url) initialChat
        // For now, create a dummy user - in real app this would come from authentication
        let dummyUser = { Id = "temp"; Nick = "Anonymous"; IsBot = false; Status = "online"; Online = true; ImageUrl = None; isMe = true }
        Connected (dummyUser, newChat), cmd
    
    | _, msg ->
        jsConsole?log ("Unhandled message in current state:", state, msg)
        state, Cmd.none

// URL navigation helper (simplified)
let urlUpdate (route: Router.Route option) (state: ChatState) : ChatState * Types.Msg Cmd =
    match route with
    | Some newRoute ->
        jsConsole?log ("Navigating to route:", newRoute)
        state, Cmd.none
    | None ->
        state, Cmd.none