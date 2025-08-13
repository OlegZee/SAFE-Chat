//these are similar to C# using statements
open canopy
open canopy.classic
open canopy.runner.classic

[<EntryPoint>]
let main _ =

    let executingDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)
    configuration.chromeDir <- executingDir
    
    // Configure for CI environment - Canopy 2.1.0 uses environment variable for headless
    if System.Environment.GetEnvironmentVariable("CI") = "true" then
        System.Environment.SetEnvironmentVariable("CANOPY_HEADLESS", "true")

    start chrome

    // define tests
    Logon.all ()
    UserCommands.all ()
    NavigationPane.all ()
    InputArea.all()
    Features.all()

    resize (1200, 800)

    run()
    quit()

    failedCount
