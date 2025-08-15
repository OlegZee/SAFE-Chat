//these are similar to C# using statements
open canopy.classic
open canopy.runner.classic
open canopy.configuration
open System.IO

open WebDriverManager
open WebDriverManager.DriverConfigs.Impl

[<EntryPoint>]
let main args =

    // Use WebDriverManager to download ChromeDriver and get its path
    let chromeDriverPath = (DriverManager ()).SetUpDriver(new ChromeConfig(), Helpers.VersionResolveStrategy.MatchingBrowser)
    
    // Tell Canopy where to find ChromeDriver
    chromeDir <- Path.GetDirectoryName chromeDriverPath

    // Configure for CI environment - Canopy 2.1.0 uses environment variable for headless
    if System.Environment.GetEnvironmentVariable "CI" = "true" then
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
