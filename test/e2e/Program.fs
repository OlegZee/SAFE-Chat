//these are similar to C# using statements
open canopy
open canopy.classic
open canopy.runner.classic
open System

open WebDriverManager
open WebDriverManager.DriverConfigs.Impl

[<EntryPoint>]
let main args =

    // Only use WebDriverManager if not in CI or if USE_SYSTEM_CHROMEDRIVER is not set
    let useSystemChromeDriver = System.Environment.GetEnvironmentVariable("USE_SYSTEM_CHROMEDRIVER") = "true"
    let isCI = System.Environment.GetEnvironmentVariable("CI") = "true"
    
    if not useSystemChromeDriver then
        (DriverManager ()).SetUpDriver(new ChromeConfig(), Helpers.VersionResolveStrategy.MatchingBrowser) |> ignore

    // Configure for CI environment - Canopy 2.1.0 uses environment variable for headless
    if isCI then
        System.Environment.SetEnvironmentVariable("CANOPY_HEADLESS", "true")

    start chrome

    // Run all test modules
    printfn "Running all test modules"
    Logon.all ()
    UserCommands.all ()
    NavigationPane.all ()
    InputArea.all()
    Features.all()

    resize (1200, 800)

    printfn "Running all tests"
    run()

    quit()

    failedCount
