using TinyCompilerForTinyBasic.Tokenization;

namespace TinyCompilerForTinyBasic.Environment;

public class DebugEnvironment : TinyBasicEnvironment
{
     private readonly HashSet<short> _breakPoints = [];
     private readonly ConsoleInterface<DebugEnvironment> _cli;

     private void PrintProgram()
     {
          Console.Clear();
          for (int i = 0; i < Program.Count; ++i)
          {
               var line = Program.GetValueAtIndex(i);
               var label = Program.GetKeyAtIndex(i);
               var lineToPrint = line.isLabeled ? line.statement.ToString() : $"({label}) {line.statement}";
               if (i == LineKeyIndex)
               {
                    Console.BackgroundColor = ConsoleColor.DarkYellow;
                    Console.Write($"—>{lineToPrint}");
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.WriteLine("");
               }
               else if (_breakPoints.Contains(label))
               {
                    Console.BackgroundColor = ConsoleColor.DarkRed;
                    Console.Write(lineToPrint);
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.WriteLine("");
               }
               else
               { Console.WriteLine(lineToPrint); }
          }
     }
     
     protected internal DebugEnvironment()
     {
          _cli = new ConsoleInterface<DebugEnvironment>(this)
          {
               InputRequestPrefix = "(DEBUG)> ",
               Fallback = (environment, command) => environment.ExecuteDirectly(string.Join(' ', command.Signature, string.Join(' ', command.Arguments))), 
          };
          AddCommands();
     }
     
     private void AddCommands()
     {
          _cli.RegisterCommand("step", (environment, command) =>
          {
               var args = command.Arguments;
               switch (args.Length)
               {
                    case 0:
                    {
                         environment.SingleStep(StepMode.In, false);
                         return;
                    }
                    case 1:
                    {
                         if (args[0] is "-f" or "--force")
                         { environment.SingleStep(StepMode.In, true); }
                         else if (Enum.TryParse<StepMode>(args[0], true, out var step))
                         { environment.SingleStep(step, false); }
                         else
                         { Console.WriteLine("Unknown argument provided"); }

                         return;
                    }
                    default:
                    {
                         if (!Enum.TryParse<StepMode>(args[0], true, out var step))
                         { Console.WriteLine("Expected a step mode as a valid first argument"); }
                         else if (args[1] is not ("-f" or "--force"))
                         { Console.WriteLine("Expected a force mode as a valid second argument"); }
                         else
                         { environment.SingleStep(step, true); }

                         return;
                    }
               }
          });
          _cli.RegisterCommand("stack", (environment, _) =>
          { environment.PrintCallStack(); });
          _cli.RegisterCommand("run", (environment, command) =>
          {
               var args = command.Arguments;
               switch (args.Length)
               {
                    case 0:
                    {
                         environment.ExecuteStatement(Program.GetValueAtIndex(LineKeyIndex).statement);
                         ++LineKeyIndex;
                         if (!CanRun())
                         { return; }
                         RunToBreakpoint();
                         PrintProgram();
                         return;
                    }
                    case 1:
                    {
                         if (!ValidateLineNumberArgument(args[0], out var line, out var statement))
                         {
                              Console.WriteLine("Expected a valid line number as an argument");
                              return;
                         }
                         if (statement.statement.StatementType == StatementType.Rem)
                         {
                              Console.WriteLine("Can't run to a comment line");
                              return;
                         }
                         RunTo(line, false);
                         PrintProgram();
                         return;
                    }
                    default:
                    {
                         if (!ValidateLineNumberArgument(args[0], out var line, out _))
                         {
                              Console.WriteLine("Expected a valid line number as a valid first argument");
                              return;
                         }
                         if (args[1] is not ("-f" or "--force"))
                         {
                              Console.WriteLine("Expected a force mode as a valid second argument");
                              return;
                         }
                         RunTo(line, true);
                         PrintProgram();
                         return;
                    }
               }
          });
          _cli.RegisterCommand("break", (_, command) =>
          {
               var args = command.Arguments;
               if (args.Length == 0)
               {
                    Console.WriteLine("Expected an argument");
                    return;
               }
               if (!ValidateLineNumberArgument(args[0], out var line, out var statement))
               {
                    Console.WriteLine("Expected a valid line number as an argument");
                    return;
               }
               if (statement.statement.StatementType == StatementType.Rem)
               {
                    Console.WriteLine("Can't place a breakpoint on a comment line");
                    return;
               }
               if (!_breakPoints.Add(line))
               { _breakPoints.Remove(line); }
               PrintProgram();
          });
          _cli.RegisterCommand("memory", (environment, command) =>
          {
               var args = command.Arguments;
               if (args.Length == 0)
               { environment.PrintMemory(null); }
               else
               {
                    var address = args[0];
                    if (!char.TryParse(address, out var charAddress))
                    { Console.WriteLine("Invalid address as an argument"); }
                    else
                    { PrintMemory(charAddress); }
               }
          });
          _cli.RegisterCommand("exit", (environment, _) =>
          {
               if (!environment.IsRunning)
               { return; }
            
               environment.IsRunning = false;
               Console.WriteLine("Execution terminated");
          });
          return;

          bool ValidateLineNumberArgument(string arg, out short line, out (Statement statement, bool isLabeled) statement)
          {
               statement = default;
               line = 0;
               if (!short.TryParse(arg, out line) || line < 0)
               { return false; }
               return Program.TryGetValue(line, out statement);
          }
     }
     
     public void Debug()
     {
          LineKeyIndex = 0;
          IsRunning = true;
          if (!CanRun())
          { 
               TerminateExecution();
               return;
          }
          if (Program.GetValueAtIndex(LineKeyIndex).statement.StatementType == StatementType.Rem)
          { SkipComments(); }
          if (!CanRun())
          { 
               TerminateExecution();
               return;
          }
          
          InterruptExecution();
     }
     
     private void RunToBreakpoint()
     {
          while (CanRun())
          {
               var label = Program.GetKeyAtIndex(LineKeyIndex);
               if (_breakPoints.Contains(label))
               { return; }
               
               Statement statement = Program.GetValueAtIndex(LineKeyIndex).statement;
               ExecuteStatement(statement);
               ++LineKeyIndex;
          }
     }
     
     private void InterruptExecution()
     {
          PrintProgram();
          while (CanRun())
          { _cli.RequestAndExecute(true); }

          if (!IsRunning) 
          { return; }
          Console.WriteLine("Runtime error: Run out of lines. Possibly missed the END or RETURN keyword?");
          TerminateExecution();
     }
     
     private void SingleStep(StepMode mode, bool ignoreBreakpoints)
     {
          var currentLine = Program.GetValueAtIndex(LineKeyIndex);
          switch (mode)
          {
               case StepMode.In:
               {
                    ExecuteStatement(currentLine.statement);
                    ++LineKeyIndex;
                    if (!CanRun())
                    { break; }
                    if (Program.GetValueAtIndex(LineKeyIndex).statement.StatementType == StatementType.Rem)
                    { SkipComments(); }
                    break;
               }
               case StepMode.Over:
               {
                    if (currentLine.statement.StatementType != StatementType.Gosub)
                    { goto case StepMode.In; }
                    
                    ExecuteStatement(currentLine.statement);
                    ++LineKeyIndex;
                    if (!CanRun())
                    { break; }
                    if (Program.GetValueAtIndex(LineKeyIndex).statement.StatementType == StatementType.Rem)
                    {
                         SkipComments();
                         if (!CanRun())
                         { break; }
                    }
                    RunFrame(ignoreBreakpoints);
                    if (CanRun() && Program.GetValueAtIndex(LineKeyIndex).statement.StatementType == StatementType.Rem)
                    { SkipComments(); }
                    break;
               }
               case StepMode.Out:
               {
                    if (ReturnStack.Count == 0)
                    { break; }
                    
                    RunFrame(ignoreBreakpoints);
                    if (CanRun() && Program.GetValueAtIndex(LineKeyIndex).statement.StatementType == StatementType.Rem)
                    { SkipComments(); }
                    break;
               }
          }
          PrintProgram();
     }

     private void RunFrame(bool ignoreBreakpoints)
     {
          var currentFrame = ReturnStack.Count;
          while (CanRun() && currentFrame <= ReturnStack.Count)
          {
               var line = Program.GetValueAtIndex(LineKeyIndex);
               ExecuteStatement(line.statement);
               ++LineKeyIndex;
               if (!ignoreBreakpoints)
               {
                    var label = Program.GetKeyAtIndex(LineKeyIndex);
                    if (_breakPoints.Contains(label))
                    { return; }
               }
          }
     }

     private void RunTo(short lineNumber, bool ignoreBreakpoints)
     {
          var label = Program.GetKeyAtIndex(LineKeyIndex);
          while (CanRun() && label != lineNumber)
          {
               var line = Program.GetValueAtIndex(LineKeyIndex);
               ExecuteStatement(line.statement);
               ++LineKeyIndex;
               label = Program.GetKeyAtIndex(LineKeyIndex);
               if (!ignoreBreakpoints && _breakPoints.Contains(label))
               { return; }
          }
     }
     
     private enum StepMode
     {
          Over,
          In,
          Out
     }

     private void PrintCallStack()
     {
          foreach (var callerLine in ReturnStack)
          {
               var line = Program.GetValueAtIndex(callerLine);
               var lineNumber = Program.GetKeyAtIndex(callerLine);
               Console.WriteLine($"Line {lineNumber}: {line.statement}");
          }
     }

     private bool CanRun() => IsRunning && LineKeyIndex < Program.Count;

     private void PrintMemory(char? address)
     {
          if (address != null)
          {
               if (address is < 'A' or > 'Z')
               {
                    Console.WriteLine("Invalid address as an argument");
                    return;
               }
               var value = Memory.ReadVariable(address.Value);
               Console.WriteLine($"{address}: {value?.ToString() ?? "Uninitialized"}");
               return;
          }

          for (var i = 'A'; i <= 'Z'; ++i)
          {
               var value = Memory.ReadVariable(i);
               Console.WriteLine($"{i}: {value?.ToString() ?? "Uninitialized"}");
          }
     }
     
     private void SkipComments()
     {
          do
          { ++LineKeyIndex; } 
          while(LineKeyIndex < Program.Count && Program.GetValueAtIndex(LineKeyIndex).statement.StatementType == StatementType.Rem);
     }
}