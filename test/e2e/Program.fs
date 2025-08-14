//these are similar to C# using statements
open canopy
open canopy.classic
open canopy.runner.classic
open System

[<EntryPoint>]
let main args =

    // Optionally skip WebDriverManager and use system ChromeDriver (recommended in CI)
    let useSystemDriver = (Environment.GetEnvironmentVariable("USE_SYSTEM_CHROMEDRIVER") = "true")
    if useSystemDriver then
        printfn "Using system-installed ChromeDriver from PATH (skipping WebDriverManager)."
    else
        // Setup ChromeDriver with WebDriverManager for automatic version compatibility
        printfn "Setting up ChromeDriver with WebDriverManager..."
        let setupChromeDriver() =
            try
                // Use WebDriverManager to set up ChromeDriver
                let driverManager = WebDriverManager.DriverManager "chrome"
                let driverPath = driverManager.SetUpDriver("chrome", "LATEST")
                printfn "ChromeDriver setup complete. Driver path: %s" driverPath
                
                // Set the driver path for Canopy
                configuration.chromeDir <- System.IO.Path.GetDirectoryName(driverPath)
                printfn "ChromeDriver directory set to: %s" configuration.chromeDir
                
            with
            | ex ->
                printfn "ChromeDriver setup failed: %s" ex.Message
                // Fallback to default behavior
                let executingDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)
                configuration.chromeDir <- executingDir
                printfn "Using fallback ChromeDriver directory: %s" executingDir

        setupChromeDriver()
    
    // Configure for CI environment - Canopy 2.1.0 uses environment variable for headless
    if System.Environment.GetEnvironmentVariable("CI") = "true" then
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
