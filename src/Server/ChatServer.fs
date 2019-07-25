module ChatServer

open System
open Microsoft.Extensions.Logging

open Akka.Actor
open Akkling
open Akkling.Persistence

open ChatTypes

module public Impl =
    // move internals here from below types definition
    ()

/// Channel is a primary store for channel info and data
type ChannelData = {
    cid: ChannelId
    name: string
    topic: string
    channelActor: IActorRef<ChannelMessage>
}
and ChannelSettings = {
    autoRemove: bool
}
and UserSessionData = {
    notifySink: ServerNotifyMessage IActorRef
}

and State = {
    channels: ChannelData list  // TODO consider map too
    sessions: Map<UserId, UserSessionData>
    lastChannelId: int
}

// notification message sent to a subscribers via notify method
and ServerNotifyMessage =
    | AddChannel of ChannelData
    | DropChannel of ChannelData

// Chan event type. I need this type to create proper actor while restoring channels
// Consider omitting parameters and just let channel store its state/settings.
type ChannelType =
    | GroupChatChannel of ChannelSettings
    | OtherChannel of Props<ChannelMessage>     // this is not persistable channel (such as About)

type ChannelCreateInfo = {
    chanId: string
    name: string
    topic: string
    chanType: ChannelType
}

type ServerEvent =
    | ChannelCreated of ChannelCreateInfo
    | ChannelDeleted of ChannelId

/// Server protocol
type ServerCommand =
    | FindChannel of (ChannelData -> bool)
    | GetOrCreateChannel of name: string * topic: string * ChannelType  // FIXME type instead of tuple
    | ListChannels of (ChannelData -> bool)
    | DumpChannels

    | NotifyLastUserLeft of ChannelId

    | StartSession of UserId * IActorRef<ServerNotifyMessage>
    | CloseSession of UserId

type ServerReplyMessage =
    | Done
    | RequestError of string
    | FoundChannel of ChannelData
    | CreatedChannel of ChannelId
    | FoundChannels of ChannelData list

type ServerMessage =
    | Event of ServerEvent
    | Command of ServerCommand

type ServerT = IActorRef<ServerMessage>

let private initialState = { channels = []; sessions = Map.empty; lastChannelId = 100 }

module private Implementation =

    let updateChannel f chanId serverState =
        let f (chan: ChannelData) = if chan.cid = chanId then f chan else chan
        in
        {serverState with channels = serverState.channels |> List.map f}

    let getChannelProps logger ({chanType = channelType; chanId = channelId}: ChannelCreateInfo) =
        match channelType with
        | GroupChatChannel settings ->
            let (true, chanId) | OtherwiseFail chanId = System.Int32.TryParse channelId
            let notify = if settings.autoRemove then Some <| box (ServerMessage.Command (NotifyLastUserLeft <| ChannelId chanId)) else None
            GroupChatChannelActor.props logger notify
        | OtherChannel props -> props

    let byChanName name c = (c:ChannelData).name = name
    let byChanId chanId c = (c:ChannelData).cid = chanId

    // verifies the name is correct
    let isValidName (name: string) =
        (String.length name) > 0 && Char.IsLetter name.[0]

open Implementation

let startServer (system: ActorSystem) (logger: ILogger) : IActorRef<ServerMessage> =

    let serverBehavior (ctx: Eventsourced<_>) =

        let update (state: State) =
            function
            | ChannelCreated ci when state.channels |> List.exists(fun {cid = ChannelId cid} -> string cid = ci.chanId) ->

                do logger.LogError ("Channel named {0} (id={1}) already exists, cannot restore", ci.name, ci.chanId)

                state

            | ChannelCreated ci ->

                let actorName = ci.chanId
                let actor = spawn ctx actorName (getChannelProps logger ci)
                let (true, chanId) | OtherwiseFail chanId = System.Int32.TryParse ci.chanId

                let newChan =  { cid = ChannelId chanId; name = ci.name; topic = ci.topic; channelActor = actor }
                let newState = { state with lastChannelId = max chanId state.lastChannelId; channels = newChan::state.channels }

                do logger.LogDebug ("Started watching {0} \"{1}\"", ci.name, ci.chanId)

                do state.sessions |> Map.iter(fun _ session -> session.notifySink <! AddChannel newChan)

                newState

            | ChannelDeleted channelId ->
                match state.channels |> List.tryFind (byChanId channelId) with
                | Some channel ->
                    do logger.LogDebug ("deleted channel {0}", channelId)
                    if ctx.IsRecovering() then
                        // FIXME the design here is to replay channels creation/destroy. See we ignore Terminated event for the same purpose
                        // Eventually I'm going to keep channel actor active until the channel is purged.
                        do logger.LogDebug ("... and sent poison pill")
                        retype channel.channelActor <! PoisonPill.Instance

                    do state.sessions |> Map.iter(fun _ session -> session.notifySink <! DropChannel channel)

                    { state with channels = state.channels |> List.filter (fun chand -> chand.cid <> channelId)}
                | None ->
                    do logger.LogError ("deleted channel {0} not found in server", channelId)
                    state

        let rec loop (state: State) : Effect<_> = actor {
            let! msg = ctx.Receive()
            // removed lifetime Terminated event tracking in March 2019

            match msg with
            | Event evt ->
                return update state evt |> loop

            | Command (NotifyLastUserLeft chanId) ->
                match state.channels |> List.tryFind (byChanId chanId) with
                | Some channel ->
                    do logger.LogDebug ("Last user left from: {0}, removing", channel.name)
                    return ChannelDeleted channel.cid |> (Event >> Persist)
                | _ ->
                    do logger.LogError ("Failed to locate channel: {0}", chanId)
                    return loop state

            | Command (FindChannel criteria) ->
                let found = state.channels |> List.tryFind criteria
                ctx.Sender() <! (found |> function |Some chan -> FoundChannel chan |_ -> RequestError "Not found")
                return ignored ()

            | Command (GetOrCreateChannel (name, topic, channelType)) ->
                match state.channels |> List.tryFind (byChanName name) with
                | Some chan ->
                    ctx.Sender() <! FoundChannel chan
                    return loop state
                | _ when isValidName name ->
                    let newChannelId = state.lastChannelId + 1
                    let event = ChannelCreated { chanId = string newChannelId; name = name; topic = topic; chanType = channelType }

                    ctx.Sender() <! CreatedChannel (ChannelId newChannelId)
                    // only persist regular channels which we know how to instantiate (persist)
                    match channelType with
                    | GroupChatChannel _ ->
                        return Persist (Event event)
                    | _ ->
                        return update state event |> loop

                | _ ->
                    ctx.Sender() <! RequestError "Invalid channel name"
                    return state |> loop

            | Command (ListChannels criteria) ->
                let found = state.channels |> List.filter criteria
                ctx.Sender() <! FoundChannels found
                return ignored()

            | Command (StartSession (user, nsink)) ->
                do logger.LogDebug ("StartSession user={0}", user)

                let newState = { state with sessions = state.sessions |> Map.add user { notifySink = nsink } }
                return loop newState

            | Command (CloseSession userid) ->
                do logger.LogDebug ("CloseSession user={0}", userid)
                
                let newState = { state with sessions = state.sessions |> Map.remove userid }
                return loop newState
            | Command DumpChannels ->
                do logger.LogDebug ("DumpChannels ({0} channels)", (List.length state.channels))
                for chan in state.channels do
                    do logger.LogDebug ("DumpChannels   {0}: \"{1}\"", chan.cid, chan.name)
                return loop state
        }
        loop initialState
    in

    let props = propsPersist serverBehavior
    let server = spawn system "ircserver" props |> retype
    server <! Command DumpChannels

    server

let getChannel criteria (server: ServerT) =
    async {
        let! (reply: ServerReplyMessage) = server <? Command (FindChannel criteria)
        match reply with
        | FoundChannel channel -> return Ok channel
        | RequestError error -> return Result.Error error
        | _ -> return Result.Error "Unknown reason"
    }

let getOrCreateChannel name topic (channelType: ChannelType) (server: ServerT) =
    async {
        let! (reply: ServerReplyMessage) = server <? Command (GetOrCreateChannel (name, topic, channelType))
        match reply with
        | CreatedChannel channelId -> return Ok channelId
        | FoundChannel channel -> return Ok channel.cid
        | RequestError error -> return Result.Error error
        | _ -> return Result.Error "Unknown reason"
    }

let listChannels criteria (server: ServerT) =
    async {
        let! (reply: ServerReplyMessage) = server <? Command (ListChannels criteria)
        match reply with
        | FoundChannels channels -> return Ok channels
        | _ -> return Result.Error "Unknown error"
    }

let startSession (server: ServerT) userId (actor: IActorRef<ServerNotifyMessage>) =
    server <! Command (StartSession (userId, actor))
