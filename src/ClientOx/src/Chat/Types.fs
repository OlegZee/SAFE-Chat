module Chat.Types

open FsChat

open Channel.Types

// Import WebSocket functionality
open WebSocket

type ChatData = {
    socket: SocketHandle
    ChannelList: Map<ChannelId,ChannelInfo>
    Channels: Map<ChannelId, ChannelData>
    NewChanName: string option   // name for new channel (part of SetCreateChanName), None - panel is hidden
} with
    static member Empty = {
        socket = { connectionState = Disconnected; url = "" }
        ChannelList = Map.empty; Channels = Map.empty; NewChanName = None}

type ChatState =
    | NotConnected
    | Connected of UserInfo * ChatData

type AppMsg =
    | Nop
    | ChannelMsg of ChannelId * Channel.Types.Msg
    | SetNewChanName of string option
    | CreateJoin
    | Join of chanId: string
    | Leave of chanId: string
    | ConnectWebSocket of string
    | DisconnectWebSocket
 
type Msg = 
    | ServerMsg of Protocol.ServerMsg
    | SendClientMsg of Protocol.ClientMsg 
    | ApplicationMsg of AppMsg
    | SocketEvent of WebSocket.SocketEvent
