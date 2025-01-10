using System.Text.RegularExpressions;

namespace TinyCompilerForTinyBasic;

public class ConsoleInterface<T>
{
    public string? InputRequestPrefix { get; init; } = null;
    public Action<T, ConsoleCommand>? Fallback { get; init; }
    private readonly Dictionary<string, Action<T, ConsoleCommand>> _availableCommands = new();
    private readonly Dictionary<string, Func<T, ConsoleCommand, Task>> _asyncCommands = new();
    private T _helpers;
    
    public ConsoleInterface(T helpers) => _helpers = helpers;
    
    public void RequestAndExecute(bool retryIfFailed)
    {
        START:
        var command = RequestInput();
        if (command is null && retryIfFailed)
        { goto START; }
        try
        { ExecuteCommand(command!); }
        catch (ArgumentException ex)
        {
            Console.WriteLine(ex.Message);
            if (retryIfFailed)
            { goto START; }
        }
    }
    
    public async Task RequestAndExecuteAsync(bool retryIfFailed)
    {
        START:
        var command = RequestInput();
        if (command is null && retryIfFailed)
        { goto START; }
        try
        { await ExecuteCommandAsync(command!); }
        catch (ArgumentException ex)
        {
            Console.WriteLine(ex.Message);
            if (retryIfFailed)
            { goto START; }
        }
    }
    
    public void RegisterCommand(string signature, Action<T, ConsoleCommand> action)
    {
        if (string.IsNullOrWhiteSpace(signature))
        { throw new ArgumentException("Command signature can't be empty"); }
        
        _availableCommands.Add(signature, action);
    }
    
    public void RegisterCommand(string signature, Func<T, ConsoleCommand, Task> func)
    {
        if (string.IsNullOrWhiteSpace(signature))
        { throw new ArgumentException("Command signature can't be empty"); }
        
        _asyncCommands.Add(signature, func);
    }
    
    private void ExecuteCommand(ConsoleCommand command)
    {
        if (_availableCommands.TryGetValue(command.Signature, out var action))
        {
            action.Invoke(_helpers, command);
            return;
        }
        
        if (Fallback is null)
        { throw new ArgumentException($"Tried to execute unregistered command with no fallback provided: {command.Signature}"); }
        Fallback(_helpers, command);
    }
    
    private Task ExecuteCommandAsync(ConsoleCommand command)
    {
        if (_asyncCommands.TryGetValue(command.Signature, out var asyncAction))
        { return asyncAction(_helpers, command); }

        if (_availableCommands.TryGetValue(command.Signature, out var action))
        {
            action.Invoke(_helpers, command);
            return Task.CompletedTask;
        }

        if (Fallback is null)
        { throw new ArgumentException($"Tried to execute unregistered command with no fallback provided: {command.Signature}"); }
        
        Fallback(_helpers, command);
        return Task.CompletedTask;
    }
    
    private ConsoleCommand? RequestInput()
    {
        if (InputRequestPrefix != null)
        { Console.Write(InputRequestPrefix); }
        var input = Console.ReadLine();
        if (string.IsNullOrEmpty(input))
        { return null; }

        var parsed = ParseInput(input);
        return parsed;
    }

    private ConsoleCommand ParseInput(string input)
    {
        if (string.IsNullOrEmpty(input))
        { throw new ArgumentException("Input cannot be empty"); }
        
        var matches = Regex.Matches(input, "\"[^\"]+\"|\\S+");
        if (matches.Count == 0)
        { throw new ArgumentException("Failed to get any single argument; bad format"); }
        
        var args = new List<string>();
        for (var i = 1; i < matches.Count; ++i)
        {
            var s = matches[i];
            args.Add(s.ToString());
        }
        return new ConsoleCommand(matches[0].ToString(), args.ToArray());
    }
}

public record ConsoleCommand(string Signature, string[] Arguments);