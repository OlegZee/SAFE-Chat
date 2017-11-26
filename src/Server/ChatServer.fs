module ChatServer

open System

open Akka.Actor
open Akkling

open ChannelFlow
open FsChat

type UserNick = UserNick of string

module ServerState =

    /// Channel is a primary store for channel info and data
    type ChannelData = {
        id: Uuid
        name: string
        topic: string
        channelActor: IActorRef<UserNick ChannelMessage>
    }

    type ServerData = {
        channels: ChannelData list
    }

type ServerControlMessage =
    | UpdateState of (ServerState.ServerData -> ServerState.ServerData)
    | FindChannel of (ServerState.ChannelData -> bool)
    | GetOrCreateChannel of name: string
    | ListChannels of (ServerState.ChannelData -> bool)

type ServerReplyMessage =
    | Done
    | RequestError of string
    | FoundChannel of ServerState.ChannelData
    | FoundChannels of ServerState.ChannelData list

module internal Helpers =
    open ServerState

    let updateChannel f chanId serverState: ServerData =
        let f chan = if chan.id = chanId then f chan else chan
        in
        {serverState with channels = serverState.channels |> List.map f}

    let byChanName name c = (c:ChannelData).name = name

    let setChannelTopic topic (chan: ChannelData) =
        {chan with topic = topic}

    // verifies the name is correct
    let isValidName (name: string) =
        (String.length name) > 0 && Char.IsLetter name.[0]

module ServerApi =
    open ServerState
    open Helpers

    /// Creates a new channel or returns existing if channel already exists
    let addChannel createChannel name topic (state: ServerData) =
        match state.channels |> List.tryFind (byChanName name) with
        | Some chan ->
            Ok (state, chan)
        | _ when isValidName name ->
            let channelActor = createChannel name
            let newChan = {
                id = Uuid.New(); name = name; topic = topic; channelActor = channelActor }
            Ok ({state with channels = newChan::state.channels}, newChan)
        | _ ->
            Error "Invalid channel name"

    let setTopic chanId newTopic state =
        Ok (state |> updateChannel (setChannelTopic newTopic) chanId)

open ServerState

/// Starts IRC server actor.
let startServer (system: ActorSystem) =

    let rec behavior (state: ServerData) (ctx: Actor<ServerControlMessage>) =
        let replyAndUpdate f = function
            | Ok (newState, reply) -> ctx.Sender() <! f reply; become (behavior newState ctx)
            | Error errtext -> ctx.Sender() <! RequestError errtext; ignored state

        function
        | UpdateState updater ->
            become (behavior (updater state) ctx)
        | FindChannel criteria ->
            let found = state.channels |> List.tryFind criteria
            match found with
            | Some chan -> ctx.Sender() <! FoundChannel chan
            | _ -> ctx.Sender() <! RequestError "Not found"
            
            ignored state
        | GetOrCreateChannel name ->
            state |> ServerApi.addChannel (createChannel system) name ""
            |> replyAndUpdate FoundChannel

        | ListChannels criteria ->
            let found = state.channels |> List.filter criteria
            ctx.Sender() <! FoundChannels found
            
            ignored state

    in
    props <| actorOf2 (behavior { channels = [] }) |> (spawn system "ircserver")

let private getChannelImpl message (server: IActorRef<ServerControlMessage>) =
    async {
        let! (reply: ServerReplyMessage) = server <? message
        match reply with
        | FoundChannel channel -> return Ok channel
        | RequestError error -> return Error error
        | _ -> return Error "Unknown reason"
    }

let getChannel criteria =
    getChannelImpl (FindChannel criteria)

let getOrCreateChannel name =
    getChannelImpl (GetOrCreateChannel name)

let listChannels criteria (server: IActorRef<ServerControlMessage>) =
    async {
        let! (reply: ServerReplyMessage) = server <? (ListChannels criteria)
        match reply with
        | FoundChannels channels -> return Ok channels
        | _ -> return Error "Unknown error"
    }

let createTestChannels system (server: IActorRef<ServerControlMessage>) =
    let addChannel name topic state =
        match state |> ServerApi.addChannel (createChannel system) name topic with
        | Ok (newstate, _) -> newstate
        | _ -> state

    let addChannels =
        addChannel "Test" "test channel #1"
        >> addChannel "Weather" "join channel to get updated"

    do server <? UpdateState addChannels |> ignore
