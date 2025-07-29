open System
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting

[<EntryPoint>]
let main argv =
    async {
        // Start the chat server (Akka.NET backend)
        do! AppOx.startChatServer()
        
        // Create and start the web host
        let webHost = AppOx.createWebHost()
        
        printfn "Starting Oxpecker web server on http://localhost:8083"
        printfn "Press Ctrl+C to shutdown"
        
        do! webHost.RunAsync() |> Async.AwaitTask
        
        return 0
    }
    |> Async.RunSynchronously