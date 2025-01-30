using TinyCompilerForTinyBasic.Parsing;
using TinyCompilerForTinyBasic.Tokenization;

namespace TinyCompilerForTinyBasic.Environment;

public class DebugEnvironment : TinyBasicEnvironment
{
     private readonly HashSet<short> _breakPoints = [];
     private readonly ConsoleInterface _cli;
     private PipeEmitter _emitter;
     
     protected internal DebugEnvironment(SortedList<short, (Statement statement, bool isLabeled)> program)
     {
          _cli = new ConsoleInterface()
          {
               InputRequestPrefix = "(DEBUG)> ",
          };
          Program = program;
          _emitter = new PipeEmitter(_breakPoints, Program);
          AddCommands();
     }
     
     private void AddCommands()
     {
          _cli.RegisterCommand("step", async (command) =>
          {
               var args = command.Arguments;
               switch (args.Length)
               {
                    case 0:
                    {
                         await SingleStep(StepMode.In, false);
                         return;
                    }
                    case 1:
                    {
                         if (args[0] is "-f" or "--force")
                         { await SingleStep(StepMode.In, true); }
                         else if (Enum.TryParse<StepMode>(args[0], true, out var step))
                         { await SingleStep(step, false); }
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
                         { await SingleStep(step, true); }

                         return;
                    }
               }
          });
          _cli.RegisterCommand("run", async (command) =>
          {
               var args = command.Arguments;
               switch (args.Length)
               {
                    case 0:
                    {
                         ExecuteStatement(Program.GetValueAtIndex(CurrentLineIndex).statement);
                         ++CurrentLineIndex;
                         await RunToBreakpoint();
                         return;
                    }
                    case 1:
                    {
                         if (!ValidateLineNumberArgument(args[0], out var line, out var statement))
                         {
                              Console.WriteLine("Expected a valid line number as an argument");
                              return;
                         }
                         if (statement.statement.Type == StatementType.Rem)
                         {
                              Console.WriteLine("Can't run to a comment line");
                              return;
                         }
                         await RunTo(line, false);
                         return;
                    }
                    default:
                    {
                         if (!ValidateLineNumberArgument(args[0], out var line, out var statement))
                         {
                              Console.WriteLine("Expected a valid line number as a valid first argument");
                              return;
                         }
                         if (statement.statement.Type == StatementType.Rem)
                         {
                              Console.WriteLine("Can't run to a comment line");
                              return;
                         }
                         if (args[1] is not ("-f" or "--force"))
                         {
                              Console.WriteLine("Expected a force mode as a valid second argument");
                              return;
                         }
                         await RunTo(line, true);
                         return;
                    }
               }
          });
          _cli.RegisterCommand("break", async (command) =>
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
                    Console.WriteLine("Expected a valid line number or \"-a\" / \"--all\" as an argument");
                    return;
               }
               if (statement.statement.Type == StatementType.Rem)
               {
                    Console.WriteLine("Can't place a breakpoint on a comment line");
                    return;
               }
               
               await _emitter.EnsureConnected();
               if (!_breakPoints.Add(line))
               { _breakPoints.Remove(line); }
               await _emitter.UpdateBreakpoint(line);
          });
          _cli.RegisterCommand("memory", (command) =>
          {
               var args = command.Arguments;
               if (args.Length == 0)
               { PrintMemory(null); }
               else
               {
                    var address = args[0];
                    if (!char.TryParse(address, out var charAddress) || charAddress is < 'A' or > 'Z')
                    { Console.WriteLine("Invalid address as an argument"); }
                    else
                    { PrintMemory(charAddress); }
               }
          });
          _cli.RegisterCommand("update", async (_) =>
          {
               await _emitter.EnsureConnected();
               await _emitter.Print();
          });
          _cli.RegisterCommand("exit", (_) =>
          {
               TerminateExecution();
               Console.WriteLine("Execution terminated");
          });
          _cli.RegisterCommand("stack", (_) => PrintCallStack());
          
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
          
          if (CanRun() && Program.GetValueAtIndex(CurrentLineIndex).statement.Type == StatementType.Rem)
          { SkipComments(); }
          if (!CanRun())
          {
               Console.WriteLine("Program does not contain any executable statements");
               TerminateExecution();
               await _emitter.Close();
               return;
          }
          
          await _emitter.EnsureConnected();
          await _emitter.UpdateCurrentLine(CurrentLineIndex);
          try
          { await InterruptExecution(); }
          catch (RuntimeException ex)
          { ex.PrintException(); }
          finally
          { await _emitter.Close(); }
     }
     
     private async Task RunToBreakpoint()
     {
          while (CanRun())
          {
               if (_breakPoints.Contains(Program.GetKeyAtIndex(CurrentLineIndex)))
               { break; }
               
               Statement statement = Program.GetValueAtIndex(CurrentLineIndex).statement;
               ExecuteStatement(statement);
               ++CurrentLineIndex;
          }
          await _emitter.EnsureConnected();
          await _emitter.UpdateCurrentLine(CurrentLineIndex);
     }

     private new async Task ExecuteDirectly(string line)
     {
          var tokens = TokenizeInput(line);
          if (tokens.Length == 0 || tokens[0] is ServiceToken { Type: ServiceType.Newline })
          { return; }

          try
          {
               var parsedLine = Parser.ParseStatement(tokens);
               if (parsedLine.Label is null)
               { base.ExecuteDirectly(parsedLine); }
               else
               { await UpdateProgram(parsedLine); }
          }
          catch (ParsingException ex)
          { ex.PrintException(); }
     }
     
     private new async Task UpdateProgram(Statement statement)
     {
          if (statement.Label is null)
          { throw new ArgumentException("Provided statement should be labeled"); }
          
          if (statement.Type == StatementType.Newline)
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
               if (CurrentLineIndex == indexOfNew && statement.Type == StatementType.Rem)
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
          if (CurrentLineIndex >= Program.Count)
          {
               await _emitter.Print();
               return;
          }
          if (CurrentLineIndex == removedIndex && Program.GetValueAtIndex(CurrentLineIndex).statement.Type == StatementType.Rem)
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
          {
               var commandRequest = await _cli.RequestAndExecuteAsync();
               if (commandRequest.executed)
               { continue; }
               
               if (commandRequest.command == null)
               { continue; }
               
               await ExecuteDirectly(string.Join(' ', commandRequest.command.Signature, 
                    string.Join(' ', commandRequest.command.Arguments)));
          }

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
                    if (CanRun() && Program.GetValueAtIndex(CurrentLineIndex).statement.Type == StatementType.Rem)
                    { SkipComments(); }
                    break;
               }
               case StepMode.Over:
               {
                    if (currentLine.statement.Type != StatementType.Gosub)
                    { goto case StepMode.In; }
                    
                    ExecuteStatement(currentLine.statement);
                    ++CurrentLineIndex;
                    if (CanRun() && Program.GetValueAtIndex(CurrentLineIndex).statement.Type == StatementType.Rem)
                    { SkipComments(); }
                    RunFrame(ignoreBreakpoints);
                    if (CanRun() && Program.GetValueAtIndex(CurrentLineIndex).statement.Type == StatementType.Rem)
                    { SkipComments(); }
                    break;
               }
               case StepMode.Out:
               {
                    if (ReturnStack.Count == 0)
                    { break; }
                    
                    RunFrame(ignoreBreakpoints);
                    if (CanRun() && Program.GetValueAtIndex(CurrentLineIndex).statement.Type == StatementType.Rem)
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
               if (!ignoreBreakpoints && _breakPoints.Contains(Program.GetKeyAtIndex(CurrentLineIndex)))
               { return; }
               var line = Program.GetValueAtIndex(CurrentLineIndex);
               ExecuteStatement(line.statement);
               ++CurrentLineIndex;
          }
     }

     private async Task RunTo(short lineNumber, bool ignoreBreakpoints)
     {
          var lineIndex = Program.IndexOfKey(lineNumber);
          while (CanRun() && CurrentLineIndex != lineIndex)
          {
               var line = Program.GetValueAtIndex(CurrentLineIndex);
               ExecuteStatement(line.statement);
               ++CurrentLineIndex;
               if (!ignoreBreakpoints && _breakPoints.Contains(Program.GetKeyAtIndex(CurrentLineIndex)))
               { break; }
          }
          await _emitter.EnsureConnected();
          await _emitter.UpdateCurrentLine(CurrentLineIndex);
     }
     
     private void PrintMemory(char? address)
     {
          if (address != null)
          {
               if (address is < 'A' or > 'Z')
               { throw new ArgumentException($"Invalid address: {address}"); }
               
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
     
     private void SkipComments()
     {
          do
          { ++CurrentLineIndex; } 
          while(CurrentLineIndex < Program.Count && Program.GetValueAtIndex(CurrentLineIndex).statement.Type == StatementType.Rem);
     }
     
     private enum StepMode
     {
          Over,
          In,
          Out
     }
}