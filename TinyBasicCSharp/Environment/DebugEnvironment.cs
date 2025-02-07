using TinyBasicCSharp.Parsing;
using TinyBasicCSharp.Tokenization;

namespace TinyBasicCSharp.Environment;

/// <summary>
/// An environment to debug TinyBasic code
/// </summary>
public partial class DebugEnvironment : TinyBasicEnvironment
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
          _cli.RegisterCommand("help", HandleHelp);
          _cli.RegisterCommand("step", HandleStep);
          _cli.RegisterCommand("run", async (command) => { await HandleRun(command); });
          _cli.RegisterCommand("break", async (command) => { await HandleBreak(command); });
          _cli.RegisterCommand("memory", HandleMemory);
          _cli.RegisterCommand("update", async (_) => { await HandleUpdate(); });
          _cli.RegisterCommand("exit", (_) => { HandleExit(); });
          _cli.RegisterCommand("stack", (_) => HandleStack());
     }

     private void HandleHelp(ConsoleCommand command)
     {
          var args = command.Arguments;
          if (args.Length == 0)
          {
               DebuggerManual.PrintHelp();
               return;
          }

          switch (args[0])
          {
               case "run":
                    DebuggerManual.PrintRun();
                    break;
               case "step":
                    DebuggerManual.PrintStep();
                    break;
               case "break":
                    DebuggerManual.PrintBreak();
                    break;
               case "stack":
                    DebuggerManual.PrintStack();
                    break;
               case "exit":
                    DebuggerManual.PrintExit();
                    break;
               case "update":
                    DebuggerManual.PrintUpdate();
                    break;
               case "memory":
                    DebuggerManual.PrintMemory();
                    break;
               default:
                    Console.WriteLine("Unknown argument for help");
                    break;
          }
     }
     
     private async Task HandleRun(ConsoleCommand command)
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
                    if (!short.TryParse(args[0], out var line) || !Program.TryGetValue(line, out var statement))
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
                    if (!short.TryParse(args[0], out var line) || !Program.TryGetValue(line, out var statement))
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
     }

     private async Task HandleBreak(ConsoleCommand command)
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
          if (!short.TryParse(args[0], out var line) || !Program.TryGetValue(line, out var statement))
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
     }
     
     private void HandleExit()
     {
          TerminateExecution();
          Console.WriteLine("Execution terminated");
     }

     private async Task HandleUpdate()
     {
          await _emitter.EnsureConnected();
          await _emitter.Print();
     }

     private void HandleMemory(ConsoleCommand command)
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
     }

     private async Task HandleStep(ConsoleCommand command)
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
     }

     /// <summary>
     /// Starts the debugger.
     /// </summary>
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
          if (tokens is null)
          { return; }
          
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
          DebuggerManual.PrintGreetings();
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
          if (currentFrame == 0)
          { throw new Exception("Tried to run frame while not in a function"); }

          do
          {
               var line = Program.GetValueAtIndex(CurrentLineIndex);
               ExecuteStatement(line.statement);
               ++CurrentLineIndex;
               if (!ignoreBreakpoints && _breakPoints.Contains(Program.GetKeyAtIndex(CurrentLineIndex)))
               { return; }
          }
          while (CanRun() && currentFrame <= ReturnStack.Count);
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
     
     private void HandleStack()
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