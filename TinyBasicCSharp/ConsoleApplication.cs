using TinyBasicCSharp.Environment;

namespace TinyBasicCSharp;

/// <summary>
/// Main class for interacting with user
/// </summary>
public partial class ConsoleApplication
{
    private TinyBasicEnvironment _environment = new();
    private ConsoleInterface _cli;
    private bool _isRunning = false;
    private string _lastUsedPath = "";
    
    public ConsoleApplication()
    {
        _cli = new ConsoleInterface()
        {
            InputRequestPrefix = "(APP)> ",
        };
        Console.CancelKeyPress += _environment.CancelHandler;
        RegisterCommands();
    }

    private void RegisterCommands()
    {
        _cli.RegisterCommand("help", (command) => HandleHelp(command.Arguments));
        _cli.RegisterCommand("save", (command) => HandleSave(command.Arguments));
        _cli.RegisterCommand("load", (command) => HandleLoad(command.Arguments));
        _cli.RegisterCommand("debug", async (command) => await HandleDebug(command.Arguments));
        _cli.RegisterCommand("exit", _ => HandleExit());
    }
    
    /// <summary>
    /// Main cycle. Asks user for input and executes commands/code
    /// </summary>
    public async Task Run()
    {
        TinyBasicManual.PrintGreetings();
        _isRunning = true;
        while (_isRunning)
        {
            var commandRequest = await _cli.RequestAndExecuteAsync();
            if (commandRequest.executed)
            { continue; }
            if (commandRequest.command is null)
            { continue; }
            _environment.ExecuteDirectly(string.Join(' ', commandRequest.command.Signature,
                string.Join(' ', commandRequest.command.Arguments)));
        }
    }

    /// <summary>
    /// Handles 'exit' command
    /// </summary>
    private void HandleExit()
    {
        _isRunning = false;
        _environment.TerminateExecution();
    }
    
    /// <summary>
    /// Handles 'load' command
    /// </summary>
    /// <param name="args">Additional arguments</param>
    private void HandleLoad(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Please specify a path to a *.bas file.");
            return;
        }
        
        var path = args[0];
        path = path.Trim('"');
        if (!FileManager.IsValidBasPath(path))
        {
            Console.WriteLine("Invalid path *.bas path");
            return;
        }

        if (OpenFile(path))
        {
            _lastUsedPath = path;
            Console.WriteLine($"Loaded {_environment.GetProgramAsStringArray().Length} lines");
        }
    }
    
    
    private bool OpenFile(string path)
    {
        if (_environment.GetProgramAsStringArray().Length == 0)
        { return Load(); }
        
        Console.WriteLine("Do you want to save current program? (y)es or (n)o");
        if (!ConsoleInterface.RequestConfirmation())
        { return Load(); }
        
        var linesToSave = _environment.GetProgramAsStringArray();
        string savePath;
        do
        {
            Console.WriteLine("Provide a *.bas path to save the program");
            savePath = Console.ReadLine()?.Trim('"') ?? "";
            while (!FileManager.IsValidBasPath(savePath))
            {
                Console.WriteLine("Invalid *.bas path");
                savePath = Console.ReadLine()?.Trim('"') ?? "";
            }
        } while (FileManager.SaveTo(linesToSave, savePath) != FileManager.SaveStatus.Success);

        return Load();

        bool Load()
        {
            var newProgram = FileManager.ReadFile(path);
            if (newProgram is not null) 
            { return _environment.LoadFile(newProgram); }
            
            Console.WriteLine($"File {path} does not exist");
            return false;
        }
    }
    
    /// <summary>
    /// Handles 'save' command
    /// </summary>
    /// <param name="args">Additional arguments</param>
    private void HandleSave(string[] args)
    {
        bool overWrite, useLastPath;
        if (args.Length == 0)
        {
            useLastPath = true;
            overWrite = false;
        }
        else if (args.Length == 1)
        { useLastPath = overWrite = args[0] is "-o" or "--overwrite"; }
        else
        {
            if (args[1] is not ("-o" or "--overwrite"))
            {
                Console.WriteLine("Expected overwrite flag as a second argument"); 
                return;
            }
            overWrite = true;
            useLastPath = false;
        }

        if (useLastPath && _lastUsedPath == "")
        {
            Console.WriteLine("Please specify a *.bas path");
            return;
        }

        var path = useLastPath ? _lastUsedPath : args[0].Trim('"');
        if (!FileManager.IsValidBasPath(path))
        {
            Console.WriteLine("Invalid path *.bas path");
            return;
        }
        
        var lines = _environment.GetProgramAsStringArray();
        if (FileManager.SaveTo(lines, path, overWrite) == FileManager.SaveStatus.Success)
        { _lastUsedPath = path; }
    }
    
    /// <summary>
    /// Handles 'debug' command
    /// </summary>
    /// <param name="args">Additional arguments</param>
    /// <returns></returns>
    private Task HandleDebug(string[] args)
    {
        DebugEnvironment? debugEnvironment = args.Length == 0 ? _environment.CreateDebugEnvironment() : OpenFile(args[0]) 
            ? _environment.CreateDebugEnvironment() : null;
        if (debugEnvironment is null)
        { return Task.CompletedTask; }
        
        Console.CancelKeyPress += _environment.CancelHandler;
        return debugEnvironment.Debug();
    }
    
    /// <summary>
    /// Handles 'help' command
    /// </summary>
    /// <param name="commands">Additional arguments</param>
    private void HandleHelp(string[] commands)
    {
        if (commands.Length < 1 || string.IsNullOrEmpty(commands[0]))
        {
            TinyBasicManual.PrintHelp();
            return;
        }

        switch (commands[0])
        {
            case "load":
                TinyBasicManual.PrintLoad();
                return;
            case "save":
                TinyBasicManual.PrintSave();
                return;
            case "PRINT":
                TinyBasicManual.PrintPrint();
                return;
            case "INPUT":
                TinyBasicManual.PrintInput();
                return;
            case "LET":
                TinyBasicManual.PrintLet();
                return;
            case "GOTO":
                TinyBasicManual.PrintGoto();
                return;
            case "GOSUB":
                TinyBasicManual.PrintGosub();
                return;
            case "IF":
                TinyBasicManual.PrintIf();
                return;
            case "RETURN":
                TinyBasicManual.PrintReturn();
                return;
            case "CLEAR":
                TinyBasicManual.PrintClear();
                return;
            case "LIST":
                TinyBasicManual.PrintList();
                return;
            case "RUN":
                TinyBasicManual.PrintRun();
                return;
            case "END":
                TinyBasicManual.PrintEnd();
                return;
            case "RND":
                TinyBasicManual.PrintRnd();
                return;
            case "REM":
                TinyBasicManual.PrintRem();
                return;
            default:
                Console.WriteLine("Unknown argument for help");
                return;
        }
    }
}