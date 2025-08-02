[![Build Status](https://travis-ci.org/AndrewEgorov/SAFE-Chat.svg?branch=dev)](https://travis-ci.org/AndrewEgorov/SAFE-Chat)

# F#chat

Sample chat application built with .NET 8, F#, Akka.NET and Fable.

![Harvest chat](docs/FsChat-login.gif "Channel view")

## Requirements

* [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or higher
* [Node.js](https://nodejs.org) 16 or higher
* npm (comes with Node.js)
* Global Fable CLI: `dotnet tool install fable --global`

## Building and running the app

### Option 1: Legacy Client (Original)
* Install JS dependencies: `yarn`
* **Move to `src/Client` folder**: `cd src\Client`
* Restore NuGet packages: `dotnet restore`
* Build client bundle: `dotnet fable webpack -p`
* **Move to `src/Server` folder**: `cd ..\Server`
* Restore NuGet packages: `dotnet restore`
* Run the server: `dotnet run`
* Head your browser to `http://localhost:8083/`

### Option 2: Modernized ClientOx (Work in Progress)
* **Use modern build script**: `build-ox.cmd` (Windows) or equivalent bash script
* Or manually:
  * **Move to `src/ClientOx` folder**: `cd src/ClientOx`
  * Install dependencies: `yarn`
  * Compile F# to JS: `fable`
  * Build bundle: `yarn build`
  * **Move to `src/Server` folder**: `cd ../Server`
  * Run the server: `dotnet run`

## Developing the app

### Legacy Client Development
* Start the server: `dev-server.cmd`
* Start client dev server: `dev-cli.cmd`
* Open browser to `http://localhost:8080/`

### Modern ClientOx Development (Recommended)
* Start the server: `dev-server.cmd`
* Start modern client dev server: `dev-clientox.cmd`
* Open browser to `http://localhost:8080/`
* Enjoy modern HMR with Webpack 5

## Modernization Status

This project has been partially modernized:

### ✅ Completed
- Upgraded to .NET 8.0
- Updated Akka.NET packages to latest versions
- Removed redundant persistence wrapper
- Created modern Fable 4.x client structure in `src/ClientOx`
- Modern webpack 5 configuration
- Updated React and Elmish packages

### 🔄 In Progress
- Complete WebSocket integration migration
- Suave to Oxpecker web framework migration
- Fix remaining compilation issues in ClientOx

### 📁 Project Structure
```
src/
├── Client/          # Legacy Fable client (working)
├── ClientOx/        # Modern Fable 4.x client (WIP)
├── Server/          # .NET 8 server with Akka.NET 
└── Shared/          # Shared protocol definitions
```

## Running integration (e2e) tests

E2e tests are based on canopy and webdriver so currently I know it runs on Windows. I have no idea how to run in non-windows environment.

* Follow the instructions above to start the server
* **Move to `test/e2e` folder**: `cd test\e2e`
* Restore NuGet packages: `dotnet restore`
* run the tests: `dotnet run`

It used to work with Expecto plugin but it's no longer included in Ionide.

## Implementation overview

### Authentication

FsChat supports both *permanent* users, authorized via goodle or github account, and *anonymous* ones, those who provide only nickname.

In order to support the google/fb authentication scenario, fill in the client/secret in the CHAT_DATA/suave.oauth.config file. In case you do not see this file, run the server once and the file will be created automatically.

### Akka streams

FsChat backend is based on Akka.Streams. The entry point is a `GroupChatFlow` module which implements the actor, serving group chat.

`UserSessionFlow` defines the flows for user and control messages, brings everything together and exposes flow for user session.

`AboutFlow` is an example of implementing channel with specific purpose, other than chatting

`ChatServer` is an actor which purpose is to keep the channel list. It's responsible for creating/dropping the channels.

`UserStore` is an actor which purpose is to know all users logged in. It supposed to be made persistent but it does not work for some reason (I created issue).

`SocketFlow` implements a flow decorating the server-side web socket.

### Akkling

Akkling is an unofficial Akka.NET API for F#. It's not just wrapper around Akka.NET API, but introduces some cool concepts such as Effects, typed actors and many more.

### Fable, Elmish

Client is written on F# with the help of Fable and Elmish (library?, framework?). Fable is absolutely mature technology, Elmish is just great.

### Communication protocol

After client is authenticated all communication between client and server is carried via WebSockets. The protocol is defined in `src/Shared/ChatProtocol.fs` file which is shared between client and server projects.

## References

* [Akkling Wiki](https://github.com/Horusiath/Akkling/wiki)
* [Fable Documentation](https://fable.io/docs/)
* [Elmish Documentation](https://elmish.github.io/elmish/)
