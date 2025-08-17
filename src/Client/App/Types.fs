module App.Types

type Msg =
  | ChatDataMsg of Connection.Types.Msg
  | SetTheme of string

type Model = {
    currentPage: Router.Route
    chatPage: Connection.Types.Model
    selectedTheme: string
  }