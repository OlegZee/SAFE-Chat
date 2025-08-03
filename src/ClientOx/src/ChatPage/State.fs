module ChatPage.State

open Elmish
open Browser.Dom

open WebSocket

open Channel.Types
open ChatPage.Types

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
        match chat.ConnectedChannels |> Map.tryFind chanId with
        | Some channel ->
            let newData, cmd = f channel
            { chat with ConnectedChannels = chat.ConnectedChannels |> Map.add chanId newData },
              cmd |> Cmd.map (fun x -> Types.ApplicationMsg (ChannelMsg (chanId, x)))
        | None ->
            console.error ("Channel %s update failed - channel not found", chanId)
            chat, Cmd.none

    let updateChan chanId (f: ChannelData -> ChannelData) (chat: ChatData) : ChatData =
        match chat.ConnectedChannels |> Map.tryFind chanId with
        | Some channel ->
            match f channel with
            | newData when newData = channel -> chat
            | newData -> { chat with ConnectedChannels = chat.ConnectedChannels |> Map.add chanId newData }
        | None ->
            console.error ("Channel %s update failed - channel not found", chanId)
            chat

    let mutable lastRequestId = 10000
    let toServerMessage x =
        let reqId = lastRequestId.ToString()
        lastRequestId <- lastRequestId + 1
        Protocol.ServerMsg.ServerCommand (reqId, x)

    let toSocketCommand (socket: SocketHandle) (msg: Protocol.ServerCommand) : Cmd<Msg> =
        let cmd = msg |> toServerMessage |> createWebSocketCmd socket |> Cmd.map SocketEvent
        cmd

    let applicationMsgUpdate (msg: AppMsg) (state: ChatData) :(ChatData * Types.Msg Cmd) =

        match msg with
        | Nop -> state, Cmd.none

        | ChannelMsg (chanId, Forward text) ->
            let message =
                match text with
                | cmd when cmd.StartsWith "/" -> Protocol.UserCommand {command = cmd; chan = chanId} |> toServerMessage
                | _ -> Protocol.UserMessage {text = text; chan = chanId}
            let cmd = message |> createWebSocketCmd state.socket |> Cmd.map SocketEvent
            state, cmd

        | ChannelMsg (chanId, Msg.Leave) ->
            state, Protocol.Leave chanId |> toSocketCommand state.socket

        | ChannelMsg (chanId, msg) ->
            let newState, cmd = state |> updateChanCmd chanId (Channel.State.update msg)
            newState, cmd

        | SetNewChanName name ->
            { state with NewChanName = name }, Cmd.none
            
        | CreateJoin ->
            match state.NewChanName with
            | Some channelName ->
                { state with NewChanName = None }, Protocol.JoinOrCreate channelName |> toSocketCommand state.socket
            | None -> state, Cmd.none

        | Join chanId ->
            state, Protocol.Join chanId |> toSocketCommand state.socket
            
        | Leave chanId ->
            state, Protocol.Leave chanId |> toSocketCommand state.socket
            
        | ConnectWebSocket url ->
            console.log ("Connecting to WebSocket:", url)
            let connectingState = { state with socket = { connectionState = Connecting; url = url } }
            connectingState, createConnectionCmd url |> Cmd.map SocketEvent
            
        | DisconnectWebSocket ->
            let closedSocket = closeSocket state.socket
            { state with socket = closedSocket }, Cmd.none

    // Handle incoming WebSocket messages
    let handleSocketEvent (event: WebSocket.SocketEvent) (state: ChatData) : ChatData * Types.Msg Cmd =
        match event with
        | ConnectionOpened socketHandle ->
            console.log "WebSocket connected, sending Greets"  
            let connectedState = { state with socket = socketHandle }
            let cmd = Protocol.Greets |> createWebSocketCmd socketHandle |> Cmd.map SocketEvent
            connectedState, cmd
            
        | ConnectionClosed ->
            console.log "WebSocket disconnected"
            let disconnectedSocket = { state.socket with connectionState = Disconnected }
            { state with socket = disconnectedSocket }, Cmd.none
            
        | ConnectionError error ->
            console.log ("WebSocket error:", error)
            let errorSocket = { state.socket with connectionState = Error error }
            { state with socket = errorSocket }, Cmd.none
            
        | MessageReceived json ->
            match deserializeClientMsg<Protocol.ClientMsg> json with
            | Some clientMsg ->
                console.log ("Received client message:", clientMsg)
                state, Cmd.ofMsg (SendClientMsg clientMsg)
            | None ->
                console.log ("Failed to parse message:", json)
                state, Cmd.none
                
        | MessageSent json ->
            console.log ("Sent message:", json)
            state, Cmd.none

    // Handle incoming server messages (converted from ClientMsg)
    let handleServerMessage (msg: Protocol.ClientMsg) (state: ChatData) : ChatData * Types.Msg Cmd =
        match msg with
        | Protocol.Hello helloInfo ->
            console.log ("Hello received:", helloInfo)
            let channels = helloInfo.channels |> List.map (fun ch -> ch.id, Conversions.mapChannel ch) |> Map.ofList
            state, Cmd.none
            
        | Protocol.CmdResponse (reqId, response) ->
            console.log ("Command response:", reqId, response)
            // Handle command responses (join, leave, etc.)
            state, Cmd.none
            
        | Protocol.ChanMsg msgInfo ->
            console.log ("Channel message:", msgInfo)
            let (authorId, message) = Conversions.mapUserMessage msgInfo
            let channelCmd = AppendUserMessage (authorId, message)
            state |> updateChan msgInfo.chan (fun chanData -> 
                fst (Channel.State.update channelCmd chanData)), Cmd.none
            
        | Protocol.ServerEvent eventInfo ->
            console.log ("Server event:", eventInfo)
            // Handle server events (user joined, left, etc.)
            state, Cmd.none

let init () : ChatState * Types.Msg Cmd =
    let socketAddr = buildWebSocketUrl window.location.host
    console.log ("Opening socket at:", socketAddr)
    NotConnected, Cmd.ofMsg (ApplicationMsg (ConnectWebSocket socketAddr))

let update (msg : Types.Msg) (state : ChatState) : ChatState * Types.Msg Cmd =
    match state, msg with
    | NotConnected, ApplicationMsg (ConnectWebSocket url) ->
        let initialChat = ChatData.Empty
        let newChat, cmd = Implementation.applicationMsgUpdate (ConnectWebSocket url) initialChat
        let dummyUser = { Id = "temp"; Nick = "Anonymous"; IsBot = false; Status = "online"; Online = true; ImageUrl = None; isMe = true }
        Connected (dummyUser, newChat), cmd
        
    | NotConnected, _ ->
        console.log ("Not connected, ignoring message:", msg)
        state, Cmd.none
        
    | Connected (user, chat), ApplicationMsg appMsg ->
        let newChat, cmd = Implementation.applicationMsgUpdate appMsg chat
        Connected (user, newChat), cmd

    | Connected (user, chat), SendClientMsg (Protocol.Hello helloInfo) ->
        let me = Conversions.mapUserInfo ((=) helloInfo.me.id) helloInfo.me
        let channels = helloInfo.channels |> List.map (fun ch -> ch.id, Conversions.mapChannel ch) |> Map.ofList
        Connected (me, { chat with ChannelList = channels }), Cmd.none

    | Connected (user, chat), SendClientMsg clientMsg ->
        let newChat, cmd = Implementation.handleServerMessage clientMsg chat
        Connected (user, newChat), cmd
        
    | Connected (user, chat), SocketEvent socketEvent ->
        let newChat, cmd = Implementation.handleSocketEvent socketEvent chat
        Connected (user, newChat), cmd
    
    | _, msg ->
        console.log ("Unhandled message in current state:", state, msg)
        state, Cmd.none

// URL navigation helper (simplified)
let urlUpdate (route: Router.Route option) (state: ChatState) : ChatState * Types.Msg Cmd =
    match route with
    | Some newRoute ->
        console.log ("Navigating to route:", newRoute)
        state, Cmd.none
    | None ->
        state, Cmd.none