module App.Types

type Msg =
  | ChatDataMsg of ChatPage.Types.Msg

type Model = {
    currentPage: Router.Route
    chat: ChatPage.Types.ChatState
  }
