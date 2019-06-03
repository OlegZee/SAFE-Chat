module App.Types

open FsChat

type Msg =
  | ChatDataMsg of Connection.Types.Msg

type Model = {
    currentPage: Router.Route
    chat: SocketBound.Model<Protocol.ServerMsg, ChatServer.Types.Model>
  }
