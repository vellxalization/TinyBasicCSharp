using TinyCompilerForTinyBasic.Environment;

namespace TinyCompilerForTinyBasic;

/// <summary>
/// Main class for interacting with user
/// </summary>
public class ConsoleApplication
{
    private TinyBasicEnvironment _environment = new();
    private ConsoleInterface<ConsoleApplication> _cli;
    private bool _isRunning = false;
    public ConsoleApplication()
    {
        _cli = new ConsoleInterface<ConsoleApplication>(this)
        {
            InputRequestPrefix = "(APP)> ",
            Fallback = (application, command) =>
            {
                application._environment.ExecuteDirectly(string.Join(' ', command.Signature, string.Join(' ', command.Arguments)));
            }
        };
        Console.CancelKeyPress += _environment.CancelHandler;
        RegisterCommands();
    }

    private void RegisterCommands()
    {
        _cli.RegisterCommand("help", (application, command) => application.PrintHelp(command.Arguments));
        _cli.RegisterCommand("execute", (application, command) => application.ExecuteFile(command.Arguments));
        _cli.RegisterCommand("debug", (application, command) => application.Debug(command.Arguments));
        _cli.RegisterCommand("exit", (application, _) => application._isRunning = false);
    }
    
    private void ExecuteFile(string[] args)
    {
        var env = CreateFileEnvironment(args);
        env.ExecuteLoadedFile();
    }

    private FileEnvironment CreateFileEnvironment(string[] args)
    {
        if (args.Length < 1)
        { throw new ArgumentException("No path to file were provided"); }
        
        var stringPath = args[0];
        if (stringPath[0] is '"' && stringPath[^1] is '"')
        { stringPath = stringPath[1..^2]; }
        var file = File.ReadAllText(stringPath);
        
        var fileEnvironment = new FileEnvironment();
        fileEnvironment.LoadFile(file);
        Console.CancelKeyPress += fileEnvironment.CancelHandler;
        return fileEnvironment;
    }
    
    /// <summary>
    /// Main cycle. Asks user for input and executes commands/code
    /// </summary>
    public void Run()
    {
        Manual.PrintGreetings();
        _isRunning = true;
        while (_isRunning)
        {
             _cli.RequestAndExecute(true);
        }
    }

    private void Debug(string[] args)
    {
        var debugEnvironment = args.Length > 0 ? CreateFileEnvironment(args).CreateDebugEnvironment() : _environment.CreateDebugEnvironment();
        debugEnvironment.Debug().Wait();
    }
    
    /// <summary>
    /// Handles 'help' command
    /// </summary>
    /// <param name="commands">User input separated by space</param>
    private void PrintHelp(string[] commands)
    {
        if (commands.Length < 2 || string.IsNullOrEmpty(commands[1]))
        {
            Manual.PrintHelp();
            return;
        }

        switch (commands[1])
        {
            case "execute":
                Manual.PrintHelpExecute();
                return;
            case "PRINT":
                Manual.PrintHelpPrint();
                return;
            case "INPUT":
                Manual.PrintHelpInput();
                return;
            case "LET":
                Manual.PrintHelpLet();
                return;
            case "GOTO":
                Manual.PrintHelpGoto();
                return;
            case "GOSUB":
                Manual.PrintHelpGosub();
                return;
            case "IF":
                Manual.PrintHelpIf();
                return;
            case "RETURN":
                Manual.PrintHelpReturn();
                return;
            case "CLEAR":
                Manual.PrintHelpClear();
                return;
            case "LIST":
                Manual.PrintHelpList();
                return;
            case "RUN":
                Manual.PrintHelpRun();
                return;
            case "END":
                Manual.PrintHelpEnd();
                return;
            default:
                Console.WriteLine("Unknown argument for help");
                return;
        }
    }
    
    /// <summary>
    /// Class for printing all possible help messages
    /// </summary>
    private static class Manual
    {
        public static void PrintGreetings()
        {
            string message = "This is a compiler for TinyBasic. Type 'help' to get started.";
            Console.WriteLine(message);
        }

        public static void PrintHelp()
        {
            string message =
                "Type 'exit' to close this window.\n" +
                "Type 'help' to display this message.\n" +
                "Type 'help [<statement>]'to see instructions for provided statement\n" +
                "Type 'execute <path>.bas' to execute .bas TinyBasic file.\n" +
                "This behaves slightly different than entered into the terminal code — type 'help execute' to learn more.\n" +
                "Type '[<lineNumber>] <statement> [<arguments>]' to execute TinyBasic code.\n" +
                "Currently this compiler supports 11 statements: IF, GOTO, GOSUB, INPUT, LET, RETURN, CLEAR, LIST, RUN, END.\n" +
                "You can either type them directly (e.g. PRINT \"HELLO WORLD\") and get immediate response or you can add a label from 1 to 32767 inclusively (e.g. 1 PRINT \"HELLO WORLD\") to write line into the memory and execute it later as a part of program.";
            Console.WriteLine(message);
        }

        public static void PrintHelpExecute()
        {
            string message = "execute <path>.bas\n" +
                             "* <path>.bas - path to TinyBasic .bas file.\n" +
                             "Reads entire file line-by-line and executes it. This will create a separate temporary environment with it's own variables and execute code there.\n" +
                             "If you're writing your TinyBasic code in a file, this compiler allows you to not every line and will do auto-increment the last line number unless the line already contains a label. For example, the following code in a file:\n\n" +
                             "1 PRINT \"HELLO WORLD!\"\n" +
                             "LET X = 10\n" +
                             "10 PRINT \"10TH LINE\"\n" +
                             "11 LET Y = 11\n" +
                             "LET Z = 12\n\n" +
                             "will automatically translate to\n\n" +
                             "1 PRINT \"HELLO WORLD!\"\n" +
                             "2 LET X = 10\n" +
                             "10 PRINT \"10TH LINE\"\n" +
                             "11 LET Y = 11\n12 LET Z = 12\n\n" +
                             "However, auto-generated labels such as 2 and 12 from the example are not allowed to be jumped to by GOTO or GOSUB statements, to avoid unexpected behavior. You should only be able to jump to user-defined labels.\n" +
                             "However, auto-generated labels WILL be overwritten if the file contains the same user-defined label. For example, the following code in a file\n\n" +
                             "PRINT \"HELLO WORLD!\"\n" +
                             "LET X = 10\n" +
                             "LET Y = 11\n" +
                             "2 LET Z = 12\n\n" +
                             "will be translated to:\n\n" +
                             "1 PRINT \"HELLO WORLD!\"\n" +
                             "2 LET Z = 12\n" +
                             "3 LET Y = 11\n\n" +
                             "Note that line 'LET X = 10' is overwritten by '2 LET Z = 12' and thus completely absent.";
            Console.WriteLine(message);
        }

        public static void PrintHelpPrint()
        {
            string message = "PRINT {<\"string\"> | <expression>} [, {<\"string\"> | <expression>} ...]\n" +
                             "* <\"string\"> - any quoted string (e.g. \"Hello World!\")\n" +
                             "* <expression> - can be a number, a variable name or an expression containing numbers, operators and variables;\n" +
                             "Outputs provided arguments in the console. Use commas to separate multiple arguments.\n\n" +
                             "Examples:\n" +
                             "* PRINT \"HELLO WORLD!\" // \"HELLO WORLD\";\n" +
                             "* PRINT 10 // \"10\";\n" +
                             "* PRINT X, \" degrees Celsius is \", (X * 9 / 5) + 32, \" degrees Fahrenheit // with X = 100: \"100 degrees Celsius is 212 degrees Fahrenheit\".";
            Console.WriteLine(message);
        }

        public static void PrintHelpInput()
        {
            string message = "INPUT {A | B |... Z} [, {A | B |... Z} ...]\n" +
                             "* {A | B |... Z} - variable name (TinyBasic supports variable names from A to Z).\n" +
                             "Requests a set of expressions from the user to be written into the corresponding variables. Providing fewer expressions than variables will result in another request until all variables aren't filled with values.\n" +
                             "On the other hand, providing more expressions will result in queuing expressions. Queued expressions are used in subsequent INPUT calls (e.g. calling \"INPUT X, Y\" and providing input \"1, 2, 3\" will result in queuing 3.\n" +
                             "Subsequent call of \"INPUT Z\" will cause 3 to be taken from the queue without requesting any input from the user).\n\n" +
                             "Examples:\n" +
                             "* INPUT X;\n" +
                             "* INPUT X, Y, Z.\n\n" +
                             "Possible user input may include:\n" +
                             "* Numbers;\n" +
                             "* Other variables;\n" +
                             "* Expressions containing numbers, operators and variables (1 + X; 1 * (S + 3)).";
            Console.WriteLine(message);
        }

        public static void PrintHelpLet()
        {
            string message = "LET {A | B | ... Z} = <expression>\n" +
                             "* {A | B |... Z} - variable name (TinyBasic supports variable names from A to Z);\n" +
                             "* <expression> - single number, variable or a complex expression containing numbers, operators and variables.\n" +
                             "Evaluates expression and writes the result into provided variable.\n\n" +
                             "Examples:\n" +
                             "* LET X = 10;\n" +
                             "* LET Y = X;\n" +
                             "* LET Z = X * (250 + Y).";
            Console.WriteLine(message);
        }

        public static void PrintHelpGoto()
        {
            string message = "GOTO <expression>\n" +
                             "* <expression> - single number, variable or a complex expression containing numbers, operators and variables.\n" +
                             "Evaluates expression and moves pointer to the value. Examples:\n" +
                             "* GOTO 20;\n" +
                             "* GOTO X;\n" +
                             "* GOTO 20 + X;";
            Console.WriteLine(message);
        }

        public static void PrintHelpGosub()
        {
            string message = "GOSUB <expression>\n" +
                             "* <expression> - Expression containing a singular number, variable or a complex expression, containing numbers, operators and variables.\n" +
                             "Evaluates expression and moves pointer to the value.\n" +
                             "Unlike GOTO, it remembers the line from which it was called, so you can RETURN and continue execution later. This allows you to create subroutines.\n\n" +
                             "Examples:\n" +
                             "* GOSUB 20;\n" +
                             "* GOSUB X;\n" +
                             "* GOSUB 20 + X;";
            Console.WriteLine(message);
        }

        public static void PrintHelpIf()
        {
            string message =
                "IF <expression1> { < | > | <= | >= | = | {<> | ><} } <expression2> THEN <statement>\n" +
                "* <expression> - single number, variable or a complex expression containing numbers, operators and variables;\n" +
                "* <statement> - statement to execute if the condition is true.\n" +
                "Checks whether the condition is true, and if so, executes the next statement.\n\n" +
                "Examples:\n" +
                "* IF 1 = 10 THEN PRINT \"TRUE\" // won't result in any execution;\n" +
                "* IF X < (10 * Y) THEN INPUT X;\n" +
                "* IF X <> (10 * Y) THEN IF X >< (20 * Y) THEN GOTO 100; // you can chain IF statements like that.\n" +
                "// You can also use both <> and >< to check that expression 1 is not equal to expression 2.";
            Console.WriteLine(message);
        }

        public static void PrintHelpReturn()
        {
            string message = "Moves pointer position to the last unreturned call of GOSUB.";
            Console.WriteLine(message);
        }

        public static void PrintHelpClear()
        {
            string message = "Removes all lines from the environment's memory.";
            Console.WriteLine(message);
        }

        public static void PrintHelpList()
        {
            string message = "Lists all lines containing in environment's memory.";
            Console.WriteLine(message);
        }

        public static void PrintHelpRun()
        {
            string message = "Executes all lines in the environment's memory, starting with the smallest label.";
            Console.WriteLine(message);
        }

        public static void PrintHelpEnd()
        {
            string message = "Terminates execution of the program. Can be used for premature termination. All programs include this command.";
            Console.WriteLine(message);
        }
    }
}