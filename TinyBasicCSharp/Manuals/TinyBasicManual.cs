using TinyBasicCSharp.Parsing;

namespace TinyBasicCSharp;

public partial class ConsoleApplication
{
    /// <summary>
    /// Class for printing help messages for interpreter
    /// </summary>
    private static class TinyBasicManual
    {
        public static void PrintGreetings()
        {
            Console.WriteLine("This is an interpreter for TinyBasic. Type 'help' to get started.");
            Console.WriteLine();
        }

        public static void PrintHelp()
        {
            Console.WriteLine("Welcome to the TinyBasic interpreter!");
            Console.WriteLine("You can write your code right here — just type any valid statement and the interpreter will execute it immediately.");
            Console.WriteLine("Example: PRINT \"HELLO WORLD!\"");
            Console.WriteLine("Or you can add a numberic label before it to store the statement in memory and use it later as a part of a larger program.");
            Console.WriteLine("Example: 1 PRINT \"HELLO WORLD!\"");
            Console.WriteLine();
            
            var statements = Parser.GetAllStatements();
            Console.Write($"Currently this interpreter supports {statements.Length} statements: ");
            for (int i = 0; i < statements.Length - 1; ++i)
            { Console.Write($"{statements[i]}, "); }
            Console.WriteLine(statements[^1]);
            Console.WriteLine("Type 'help <statement>' to see instructions for provided statement.");
            Console.WriteLine();
            
            var functions = FunctionParser.GetFunctionNames();
            Console.Write($"Currently this interpreter supports {functions.Length} functions: ");
            for (int i = 0; i < functions.Length - 1; ++i)
            { Console.Write($"{functions[i]}, "); }
            Console.WriteLine(functions[^1]);
            Console.WriteLine("Functions can be called anywhere in expressions instead of using a variable or a number");
            Console.WriteLine("Type 'help <function>' to see instructions for provided function.");
            Console.WriteLine();
            Console.WriteLine("Here are the rest of the commands");
            Console.WriteLine("Type 'exit' to close this window");
            Console.WriteLine("Type 'help' to display this message");
            Console.WriteLine("Type 'debug' to start the debugger.");
            Console.WriteLine("Type 'load <path>.bas' to load a TinyBasic file. Type 'help load' to learn more.");
            Console.WriteLine("Type 'save' to save current program. Type 'help save' to learn more.");
            Console.WriteLine();
        }

        public static void PrintSave()
        {
            Console.WriteLine("save [<path>.bas] [{-o | --overwrite}]");
            Console.WriteLine("<path>.bas - *.bas file to save current program to;");
            Console.WriteLine("-o, --overwrite - overwrite flag. If used, will automatically overwrite the existing file.");
            Console.WriteLine("Saves the program. If no path is specified, will try to use the last valid path used when loading or saving.");
            Console.WriteLine();
        }
        
        public static void PrintLoad()
        {
            Console.WriteLine("load <path>.bas");
            Console.WriteLine("* <path>.bas - path to TinyBasic .bas file.");
            Console.WriteLine("Reads the entire file line by line and loads it into memory.");
            Console.WriteLine("This interpreter allows you not to number each line and will automatically increment new line if they aren't already labeled. For example, the following code in a file:");
            Console.WriteLine("1 PRINT \"HELLO WORLD!\"");
            Console.WriteLine("LET X = 10");
            Console.WriteLine("10 PRINT \"10TH LINE\"");
            Console.WriteLine("11 LET Y = 11");
            Console.WriteLine("LET Z = 12");
            Console.WriteLine();
            Console.WriteLine("will automatically translate to");
            Console.WriteLine();
            Console.WriteLine("1 PRINT \"HELLO WORLD!\"");
            Console.WriteLine("(2) LET X = 10");
            Console.WriteLine("10 PRINT \"10TH LINE\"");
            Console.WriteLine("11 LET Y = 11");
            Console.WriteLine("(12) LET Z = 12");
            Console.WriteLine();
            Console.WriteLine("Auto-generated labels are shown in parentheses.");
            Console.WriteLine("Important note 1: auto-generated labels cannot be jumped to. This restriction exists to prevent unexpected behavior.");
            Console.WriteLine("Important note 2: manually created labels have higher priority than auto-generated ones, meaning that they will automatically overwrite them. For example, the following code in a file");
            Console.WriteLine("PRINT \"HELLO WORLD!\"");
            Console.WriteLine("LET X = 10");
            Console.WriteLine("LET Y = 11");
            Console.WriteLine("2 LET Z = 12");
            Console.WriteLine();
            Console.WriteLine("will be translated to:");
            Console.WriteLine();
            Console.WriteLine("(1) PRINT \"HELLO WORLD!\"");
            Console.WriteLine("2 LET Z = 12");
            Console.WriteLine("(3) LET Y = 11");
            Console.WriteLine("Note that line 'LET X = 10' is overwritten by '2 LET Z = 12' and thus completely absent.");
            Console.WriteLine();
        }

        public static void PrintPrint()
        {
            Console.WriteLine("PRINT {<\"string\"> | <expression>} [, {<\"string\"> | <expression>} ...]");
            Console.WriteLine("* <\"string\"> - any quoted string (e.g. \"Hello World!\")");
            Console.WriteLine("* <expression> - single number, variable, function call or a complex expression containing all of the above.");
            Console.WriteLine("Outputs provided arguments in the console. Use commas to separate multiple arguments.");
            Console.WriteLine("Examples:");
            Console.WriteLine("* PRINT \"HELLO WORLD!\" // \"HELLO WORLD\";");
            Console.WriteLine("* PRINT 10 // \"10\";");
            Console.WriteLine("* PRINT X, \" degrees Celsius is \", (X * 9 / 5) 32, \" degrees Fahrenheit // with X = 100: \"100 degrees Celsius is 212 degrees Fahrenheit\".");
            Console.WriteLine();
        }

        public static void PrintInput()
        {
            Console.WriteLine("INPUT {A | B |... Z} [, {A | B |... Z} ...]");
            Console.WriteLine("* {A | B |... Z} - variable name (TinyBasic supports variable names from A to Z).");
            Console.WriteLine("Requests a set of expressions from the user to be written into the corresponding variables.");
            Console.WriteLine("Providing fewer expressions than variables will result in another request until all variables aren't filled with values.");
            Console.WriteLine("On the other hand, providing more expressions will result in queuing expressions.");
            Console.WriteLine("Queued expressions are used in subsequent INPUT calls (e.g. calling \"INPUT X, Y\" and providing input \"1, 2, 3\" will result in queuing 3.");
            Console.WriteLine("Subsequent call of \"INPUT Z\" will cause 3 to be taken from the queue without requesting any input from the user).");
            Console.WriteLine("Examples:");
            Console.WriteLine("* INPUT X;");
            Console.WriteLine("* INPUT X, Y, Z.");
            Console.WriteLine();
            Console.WriteLine("Possible user input may include:");
            Console.WriteLine("* Numbers;");
            Console.WriteLine("* Variables;");
            Console.WriteLine("* Function calls;");
            Console.WriteLine("* Expressions containing all of the above.");
            Console.WriteLine();
        }

        public static void PrintLet()
        {
            Console.WriteLine("LET {A | B | ... Z} = <expression>");
            Console.WriteLine("* {A | B |... Z} - variable name (TinyBasic supports variable names from A to Z);");
            Console.WriteLine("* <expression> - single number, variable, function call or a complex expression containing all of the above.");
            Console.WriteLine("Evaluates expression and writes the result into provided variable.");
            Console.WriteLine("Examples:");
            Console.WriteLine("* LET X = 10;");
            Console.WriteLine("* LET Y = X;");
            Console.WriteLine("* LET Z = X * (250 Y).");
            Console.WriteLine();
        }

        public static void PrintGoto()
        {
            Console.WriteLine("GOTO <1-32767>");
            Console.WriteLine("Moves the pointer to the specified line.");
            Console.WriteLine("* GOTO 20;");
            Console.WriteLine();
        }

        public static void PrintGosub()
        {
            Console.WriteLine("GOSUB <1-32767>");
            Console.WriteLine("Moves the pointer to the specified line.");
            Console.WriteLine("Unlike GOTO, it remembers the line from which it was called, so you can RETURN and continue execution later. This allows you to create subroutines.");
            Console.WriteLine("Examples:");
            Console.WriteLine("* GOSUB 20;");
            Console.WriteLine();
        }

        public static void PrintIf()
        {
            Console.WriteLine("IF <expression> { < | > | <= | >= | = | {<> | ><} } <expression> THEN <statement>");
            Console.WriteLine("* <expression> - single number, variable, function call or a complex expression containing all of the above;");
            Console.WriteLine("* <statement> - statement to execute if the condition is true.");
            Console.WriteLine("Checks whether the condition is true, and if so, executes the next statement.");
            Console.WriteLine("Examples:");
            Console.WriteLine("* IF 1 = 10 THEN PRINT \"TRUE\" // won't result in any execution;");
            Console.WriteLine("* IF X < (10 * Y) THEN INPUT X;");
            Console.WriteLine("* IF X <> (10 * Y) THEN IF X >< (20 * Y) THEN GOTO 100; // you can chain IF statements like that.");
            Console.WriteLine("You can also use both <> and >< for the not equal check.");
            Console.WriteLine();
        }

        public static void PrintRem()
        {
            Console.WriteLine("REM [<string>]");
            Console.WriteLine("* <string> - any form of text");
            Console.WriteLine("Comment. A line with no functional meaning, which exists only to provide more info about the code.");
            Console.WriteLine();
        }

        public static void PrintRnd()
        {
            Console.WriteLine("RND(<expression>)");
            Console.WriteLine("* <expression> - single number, variable, function call or a complex expression containing all of the above.");
            Console.WriteLine("Generates a random number between 0 (inclusive) and <expression> (exclusive).");
            Console.WriteLine();
        }
        
        public static void PrintReturn()
        {
            Console.WriteLine("Moves the pointer to the last unreturned GOSUB call.");
            Console.WriteLine();
        }

        public static void PrintClear()
        {
            Console.WriteLine("Removes all lines from the environment memory.");
            Console.WriteLine();
        }

        public static void PrintList()
        {
            Console.WriteLine("Prints all lines in the environment memory.");
            Console.WriteLine();
        }

        public static void PrintRun()
        {
            Console.WriteLine("Executes each line starting from the line with the smallest label.");
            Console.WriteLine();
        }

        public static void PrintEnd()
        {
            Console.WriteLine("Terminates execution of the program. Can be used for premature termination. All programs must include this command.");
            Console.WriteLine();
        }
    }
}