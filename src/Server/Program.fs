open System.IO

open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Giraffe
open Microsoft.Extensions.Logging

type CmdArgs = { ClientPath: string }

let (</>) a b = Path.Combine(a, b)

[<EntryPoint>]
let main argv =

    let args =
        let parse f str = match f str with (true, i) -> Some i | _ -> None

        // let (|Port|_|) = parse System.UInt16.TryParse
        // let (|IPAddress|_|) = parse System.Net.IPAddress.TryParse

        //default bind to 127.0.0.1:8083
        let defaultArgs = {
            // IP = System.Net.IPAddress.Loopback; Port = 8083us
            ClientPath = ".." </> "Client" </> "public"
            }

        let rec parseArgs b args =
            match args with
            | [] -> b
            // | "--ip" :: IPAddress ip :: xs -> parseArgs { b with IP = ip } xs
            // | "--port" :: Port p :: xs -> parseArgs { b with Port = p } xs
            | "-cp" :: clientPath :: xs
            | "--clientpath" :: clientPath :: xs -> parseArgs { b with ClientPath = clientPath } xs
            | invalidArgs ->
                printfn "error: invalid arguments %A" invalidArgs
                printfn "Usage:"
                // printfn "    --ip ADDRESS      ip address (Default: %O)" defaultArgs.IP
                // printfn "    --port PORT       port (Default: %i)" defaultArgs.Port
                printfn "    --clientpath PATH client bundle path(Default: %s)" defaultArgs.ClientPath
                exit 1

        argv |> List.ofArray |> parseArgs defaultArgs

    // let logger = Logging.Targets.create Logging.Verbose [| "Suave" |]

    // let app = App.root >=> logWithLevelStructured Logging.Info logger logFormatStructured

    // let config =
    //     { defaultConfig with
    //         logger = Targets.create LogLevel.Debug [|"ServerCode"; "Server" |]
    //         bindings = [ HttpBinding.create HTTP args.IP args.Port ]
    //         cookieSerialiser = TweakingSuave.JsonNetCookieSerialiser()
    //         homeFolder = args.ClientPath |> (Path.GetFullPath >> Some)

    //         serverKey = App.Secrets.readCookieSecret()
    //     }

    let webApp =
        choose [
            route "/ping"   >=> text "pong"
            route "/"       >=> htmlFile "/pages/index.html" ]

    let configureLogging (builder : ILoggingBuilder) =
        builder
            // .AddFilter(fun l -> l.Equals LogLevel.Error)
           .AddConsole()
           .AddDebug() |> ignore

    let configureApp (app : IApplicationBuilder) =
        // Add Giraffe to the ASP.NET Core pipeline
        app.UseGiraffe webApp

    let configureServices (services : IServiceCollection) =
        // Add Giraffe dependencies
        services.AddGiraffe() |> ignore

    let host =
        WebHostBuilder()
            .UseKestrel()
            .Configure(Action<IApplicationBuilder> configureApp)
            .ConfigureServices(configureServices)
            .ConfigureLogging(configureLogging)
            .Build()
    // let application = async {
    //     let _, webServer = startWebServerAsync config app
    //     do! App.startChatServer()
    //     return ()
    // }

    let cts = new System.Threading.CancellationTokenSource()
    host.StartAsync cts.Token |> ignore

    //kill the server
    printfn "type 'q' to gracefully stop"
    while "q" <> System.Console.ReadLine() do ()
    cts.Cancel()

    0 // return an integer exit code
