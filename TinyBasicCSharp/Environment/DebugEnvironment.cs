using TinyCompilerForTinyBasic.Tokenization;

namespace TinyCompilerForTinyBasic.Environment;

public class DebugEnvironment : TinyBasicEnvironment
{
     private readonly HashSet<short> _breakPoints = [];
     private readonly ConsoleInterface<DebugEnvironment> _cli;
     private PipeEmitter _emitter;
     
     protected internal DebugEnvironment(SortedList<short, (Statement statement, bool isLabeled)> program)
     {
          _cli = new ConsoleInterface<DebugEnvironment>(this)
          {
               InputRequestPrefix = "(DEBUG)> ",
               Fallback = (environment, command) => environment.ExecuteDirectly(string.Join(' ', command.Signature, string.Join(' ', command.Arguments))), 
          };
          Program = program;
          AddCommands();
          _emitter = new PipeEmitter(_breakPoints, Program);
     }
     
     private void AddCommands()
     {
          _cli.RegisterCommand("step", async (environment, command) =>
          {
               var args = command.Arguments;
               switch (args.Length)
               {
                    case 0:
                    {
                         await environment.SingleStep(StepMode.In, false);
                         return;
                    }
                    case 1:
                    {
                         if (args[0] is "-f" or "--force")
                         { await environment.SingleStep(StepMode.In, true); }
                         else if (Enum.TryParse<StepMode>(args[0], true, out var step))
                         { await environment.SingleStep(step, false); }
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
                         { await environment.SingleStep(step, true); }

                         return;
                    }
               }
          });
          _cli.RegisterCommand("stack", (environment, _) =>
          { environment.PrintCallStack(); });
          _cli.RegisterCommand("run", async (environment, command) =>
          {
               var args = command.Arguments;
               switch (args.Length)
               {
                    case 0:
                    {
                         environment.ExecuteStatement(Program.GetValueAtIndex(CurrentLineIndex).statement);
                         ++CurrentLineIndex;
                         if (!CanRun())
                         { return; }
                         environment.RunToBreakpoint();
                         await _emitter.EnsureConnected();
                         await _emitter.UpdateCurrentLine(CurrentLineIndex);
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
                         environment.RunTo(line, false);
                         await _emitter.EnsureConnected();
                         await _emitter.UpdateCurrentLine(CurrentLineIndex);
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
                         environment.RunTo(line, true);
                         await _emitter.EnsureConnected();
                         await _emitter.UpdateCurrentLine(CurrentLineIndex);
                         return;
                    }
               }
          });
          _cli.RegisterCommand("break", async (_, command) =>
          {
               var args = command.Arguments;
               if (args.Length == 0)
               {
                    Console.WriteLine("Expected an argument");
                    return;
               }
               if (args[0] is "-a" or "--all")
               {
                    foreach (var breakPoint in _breakPoints.Order())
                    { Console.WriteLine($"Breakpoint at: {breakPoint}"); }
                    return;
               }
               if (!ValidateLineNumberArgument(args[0], out var line, out var statement))
               {
                    Console.WriteLine("Expected a valid line number or \"-a\"/\"--all\" as an argument");
                    return;
               }
               if (statement.statement.StatementType == StatementType.Rem)
               {
                    Console.WriteLine("Can't place a breakpoint on a comment line");
                    return;
               }
               
               await _emitter.EnsureConnected();
               if (!_breakPoints.Add(line))
               { _breakPoints.Remove(line); }
               await _emitter.UpdateBreakpoint(line);
          });
          _cli.RegisterCommand("update", async (env, _) => await env._emitter.Print());
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
     
     public async Task Debug()
     {
          CurrentLineIndex = 0;
          IsRunning = true;
          if (!CanRun())
          { 
               TerminateExecution();
               return;
          }
          if (Program.GetValueAtIndex(CurrentLineIndex).statement.StatementType == StatementType.Rem)
          { SkipComments(); }
          if (!CanRun())
          { 
               TerminateExecution();
               return;
          }

          await _emitter.EnsureConnected();
          await _emitter.UpdateCurrentLine(CurrentLineIndex);
          
          try
          { await InterruptExecution(); }
          catch (RuntimeException ex)
          { Console.WriteLine($"Runtime error:\n >{ex.Message}"); }
          finally
          { await _emitter.Close(); }
     }
     
     private void RunToBreakpoint()
     {
          while (CanRun())
          {
               if (_breakPoints.Contains(Program.GetKeyAtIndex(CurrentLineIndex)))
               { return; }
               
               Statement statement = Program.GetValueAtIndex(CurrentLineIndex).statement;
               ExecuteStatement(statement);
               ++CurrentLineIndex;
          }
     }

     protected override async void UpdateProgram(Statement statement)
     {
          if (statement.Label is null)
          { throw new ArgumentException("Provided statement should be labeled"); }
          
          if (statement.StatementType == StatementType.Newline)
          { await RemoveLine(statement.Label.Value); }
          else
          { await AddOrUpdateLine(statement); }
     }

     private async Task AddOrUpdateLine(Statement statement)
     {
          await _emitter.EnsureConnected();
          var label = statement.Label!.Value;
          var insertedNew = Program.TryAdd(label, (statement, true));
          var indexOfNew = (short)Program.IndexOfKey(label);
          if (insertedNew)
          {
               await _emitter.AddLine(statement, label);
               if (indexOfNew <= CurrentLineIndex)
               {
                    ++CurrentLineIndex; // adjust current index
                    await _emitter.UpdateCurrentLine(CurrentLineIndex);
               }
          }
          else
          {
               Program.SetValueAtIndex(indexOfNew, (statement, true));
               await _emitter.UpdateLine(statement, indexOfNew);
               if (CurrentLineIndex == indexOfNew && statement.StatementType == StatementType.Rem)
               {
                    SkipComments();
                    await _emitter.UpdateCurrentLine(CurrentLineIndex);
               }
          }
          await _emitter.Print();
     }

     private async Task RemoveLine(short label)
     {
          await _emitter.EnsureConnected();
          if (_breakPoints.Remove(label))
          { await _emitter.UpdateBreakpoint(label); }
          
          var removedIndex = (short)Program.IndexOfKey(label);
          Program.RemoveAt(removedIndex);
          await _emitter.RemoveLine(removedIndex);

          if (CurrentLineIndex == removedIndex && Program.GetValueAtIndex(CurrentLineIndex).statement.StatementType == StatementType.Rem)
          {
               SkipComments();
               await _emitter.UpdateCurrentLine(CurrentLineIndex);
          }
          else if (CurrentLineIndex > removedIndex)
          {
               --CurrentLineIndex; // adjust current index
               await _emitter.UpdateCurrentLine(CurrentLineIndex);
          }
          await _emitter.Print();
     }

     private async Task InterruptExecution()
     {
          while (CanRun())
          { await _cli.RequestAndExecuteAsync(true); }

          if (!IsRunning) 
          { return; }
          Console.WriteLine("Runtime error: Run out of lines. Possibly missed the END or RETURN keyword?");
          TerminateExecution();
     }
     
     private async Task SingleStep(StepMode mode, bool ignoreBreakpoints)
     {
          var currentLine = Program.GetValueAtIndex(CurrentLineIndex);
          switch (mode)
          {
               case StepMode.In:
               {
                    ExecuteStatement(currentLine.statement);
                    ++CurrentLineIndex;
                    if (!CanRun())
                    { break; }
                    if (Program.GetValueAtIndex(CurrentLineIndex).statement.StatementType == StatementType.Rem)
                    { SkipComments(); }
                    break;
               }
               case StepMode.Over:
               {
                    if (currentLine.statement.StatementType != StatementType.Gosub)
                    { goto case StepMode.In; }
                    
                    ExecuteStatement(currentLine.statement);
                    ++CurrentLineIndex;
                    if (!CanRun())
                    { break; }
                    if (Program.GetValueAtIndex(CurrentLineIndex).statement.StatementType == StatementType.Rem)
                    {
                         SkipComments();
                         if (!CanRun())
                         { break; }
                    }
                    RunFrame(ignoreBreakpoints);
                    if (CanRun() && Program.GetValueAtIndex(CurrentLineIndex).statement.StatementType == StatementType.Rem)
                    { SkipComments(); }
                    break;
               }
               case StepMode.Out:
               {
                    if (ReturnStack.Count == 0)
                    { break; }
                    
                    RunFrame(ignoreBreakpoints);
                    if (CanRun() && Program.GetValueAtIndex(CurrentLineIndex).statement.StatementType == StatementType.Rem)
                    { SkipComments(); }
                    break;
               }
          }
          await _emitter.EnsureConnected();
          await _emitter.UpdateCurrentLine(CurrentLineIndex);
     }

     private void RunFrame(bool ignoreBreakpoints)
     {
          var currentFrame = ReturnStack.Count;
          while (CanRun() && currentFrame <= ReturnStack.Count)
          {
               var line = Program.GetValueAtIndex(CurrentLineIndex);
               ExecuteStatement(line.statement);
               ++CurrentLineIndex;
               if (ignoreBreakpoints) 
               { continue; }
               if (_breakPoints.Contains(Program.GetKeyAtIndex(CurrentLineIndex)))
               { return; }
          }
     }

     private void RunTo(short lineNumber, bool ignoreBreakpoints)
     {
          var lineIndex = Program.IndexOfKey(lineNumber);
          while (CanRun() && CurrentLineIndex != lineIndex)
          {
               var line = Program.GetValueAtIndex(CurrentLineIndex);
               ExecuteStatement(line.statement);
               ++CurrentLineIndex;
               if (!ignoreBreakpoints && _breakPoints.Contains(Program.GetKeyAtIndex(CurrentLineIndex)))
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

     private bool CanRun() => IsRunning && CurrentLineIndex < Program.Count;

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
          { ++CurrentLineIndex; } 
          while(CurrentLineIndex < Program.Count && Program.GetValueAtIndex(CurrentLineIndex).statement.StatementType == StatementType.Rem);
     }
}