using TinyCompilerForTinyBasic.Environment;

namespace TinyCompilerForTinyBasic;

public class ConsoleApplication
{
    private TinyBasicEnvironment _environment = new();

    public ConsoleApplication()
    {
        Console.CancelKeyPress += _environment.CancelHandler;
    }
    
    public void Run()
    {
        PrintGreetings();
        bool isRunning = true;
        while (isRunning)
        {
            string input = Console.ReadLine() ?? "";
            if (string.IsNullOrEmpty(input))
            { continue; }

            string[] commands = input.Split(" ");
            switch (commands[0])
            {
                case "exit":
                    isRunning = false;
                    break;
                case "help":
                    PrintHelp();
                    break;
                case "execute":
                    if (commands.Length < 1)
                    {
                        Console.WriteLine("No path were provided");
                        break;
                    }
                    ExecuteFile(commands[1]);
                    break;
                default:
                    try
                    { _environment.ExecuteDirectly(input); }
                    catch (TokenizationException tokenizationException)
                    { Console.WriteLine($"Error while tokenizing line: {tokenizationException.Message}"); }
                    catch (ParsingException parsingException)
                    { Console.WriteLine($"Error while parsing line: {parsingException.Message}"); }
                    catch (RuntimeException runtimeException)
                    { Console.WriteLine($"Error while running code: {runtimeException.Message}"); }
                    catch (Exception otherException)
                    { Console.WriteLine($"Unknown error: {otherException.Message}"); }
                    break;
            }
        }
    }

    private void PrintHelp()
    {
        string message = "line ::= number statement CR | statement CR\n \n    statement ::= PRINT expr-list\n                  IF expression relop expression THEN statement\n                  GOTO expression\n                  INPUT var-list\n                  LET var = expression\n                  GOSUB expression\n                  RETURN\n                  CLEAR\n                  LIST\n                  RUN\n                  END\n \n    expr-list ::= (string|expression) (, (string|expression) )*\n \n    var-list ::= var (, var)*\n \n    expression ::= (+|-|ε) term ((+|-) term)*\n \n    term ::= factor ((*|/) factor)*\n \n    factor ::= var | number | (expression)\n \n    var ::= A | B | C ... | Y | Z\n \n    number ::= digit digit*\n \n    digit ::= 0 | 1 | 2 | 3 | ... | 8 | 9\n \n    relop ::= < (>|=|ε) | > (<|=|ε) | =\n\n    string ::= \" ( |!|#|$ ... -|.|/|digit|: ... @|A|B|C ... |X|Y|Z)* \"";
        Console.WriteLine(message);
    }

    private void PrintGreetings()
    {
        string message = "This is a compiler for TinyBasic. You can type: 'help' to see TinyBasic documentation; " +
                         "'execute pathToProgram' to execute TinyBasic program; " +
                         "'exit' to close compiler; " +
                         "or just start writing your TinyBasic code here line-by-line";
        Console.WriteLine(message);
    }
    
    private void ExecuteFile(string filePath)
    {
        var env = new TinyBasicEnvironment();
        Console.CancelKeyPress += env.CancelHandler;
        
        FileInfo fileInfo;
        try
        { fileInfo = new FileInfo(filePath); }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading file: {ex.Message}");
            return;
        }
        
        string sourceCode = File.ReadAllText(fileInfo.FullName);
        try
        { env.ExecuteFile(sourceCode); }
        catch (TokenizationException tokenizationException)
        { Console.WriteLine($"Error while tokenizing file: {tokenizationException.Message}"); }
        catch (ParsingException parsingException)
        { Console.WriteLine($"Error while parsing file: {parsingException.Message}"); }
        catch (RuntimeException runtimeException)
        { Console.WriteLine($"Error while running code: {runtimeException.Message}"); }
        catch (Exception otherException)
        { Console.WriteLine($"Unknown error: {otherException.Message}"); }
        Console.CancelKeyPress -= env.CancelHandler;
    }
}