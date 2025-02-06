namespace TinyBasicCSharp.Environment;

public partial class DebugEnvironment
{
    /// <summary>
    /// Class for printing help messages for debugger
    /// </summary>
    private static class DebuggerManual
    {
        public static void PrintGreetings()
        {
            Console.WriteLine("Welcome to the TinyBasic debugger! Type 'help' to get started.");
            Console.WriteLine();
        }

        public static void PrintHelp()
        {
            Console.WriteLine("Currently this debugger supports the following commands:");
            Console.WriteLine("* step;");
            Console.WriteLine("* run;");
            Console.WriteLine("* break;");
            Console.WriteLine("* memory;");
            Console.WriteLine("* stack;");
            Console.WriteLine("* update;");
            Console.WriteLine("* exit.");
            Console.WriteLine("Type 'help <command>' to learn more about each command.");
            Console.WriteLine();
        }
        
        public static void PrintStep()
        {
            Console.WriteLine("step [ {in | out | over} ] [ {-f | --force} ]");
            Console.WriteLine("* in, out, over - step mode;");
            Console.WriteLine("* -f, --force - force flag. If used, will ignore any placed breakpoints.");
            Console.WriteLine("Will perform a step depending on the provided mode:");
            Console.WriteLine("* IN - performs a single step. When used on a GOSUB statement, moves inside a subroutine. Default mode if no mode argument is specified.");
            Console.WriteLine("* OUT - when used inside a subroutine, executes the rest of the code and stops at the next statement after GOSUB call.");
            Console.WriteLine("* OVER - when used on a GOSUB statement, moves to the next statement while still executing subroutine.");
            Console.WriteLine();
        }
        
        public static void PrintRun()
        {
            Console.WriteLine("run [<1-32767>] [ {-f | --force} ]");
            Console.WriteLine("* <1-32767> - line number at where execution will stop;");
            Console.WriteLine("* -f, --force - force flag. If used, will ignore any placed breakpoints.");
            Console.WriteLine("Runs multiple lines. If no line is specified, runs to the next breakpoint.");
            Console.WriteLine();
        }

        public static void PrintBreak()
        {
            Console.WriteLine("break { [<1-32767>] | { -a | --all } }");
            Console.WriteLine("* <1-32767> - line number where breakpoint should be set/removed;");
            Console.WriteLine("* -a, --all - all flag. Prints all breakpoints.");
            Console.WriteLine("Sets or removes breakpoint at the specified line.");
            Console.WriteLine();
        }

        public static void PrintMemory()
        {
            Console.WriteLine("memory [ {A | B | ... Z } ]");
            Console.WriteLine("* {A | B | ... Z } - memory address.");
            Console.WriteLine("Prints values of all variables in memory. If address is specified, prints only value of the address only.");
            Console.WriteLine();
        }
        
        public static void PrintUpdate()
        {
            Console.WriteLine("Force update of the debugger's console.");
            Console.WriteLine();
        }
        
        public static void PrintStack()
        {
            Console.WriteLine("Prints the GOSUB call stack.");
            Console.WriteLine();
        }

        public static void PrintExit()
        {
            Console.WriteLine("Closes debugger.");
            Console.WriteLine();
        }
    }
}