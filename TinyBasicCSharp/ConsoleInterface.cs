using System.Text.RegularExpressions;

namespace TinyBasicCSharp;

public class ConsoleInterface
{
    public string? InputRequestPrefix { get; init; } = null;
    private readonly Dictionary<string, Action<ConsoleCommand>> _availableCommands = new();
    private readonly Dictionary<string, Func<ConsoleCommand, Task>> _asyncCommands = new();
    
    /// <summary>
    /// Requests and tries to execute command
    /// </summary>
    /// <returns>bool - has any command been executed;
    /// ConsoleCommand? - attempted command</returns>
    public (bool, ConsoleCommand?) RequestAndExecute()
    {
        var command = RequestInput();
        if (command is null)
        { return (false, null); }
        
        var result = ExecuteCommand(command);
        return (result, command);
    }
    
    /// <summary>
    /// Requests and tries to execute command asynchronously
    /// </summary>
    /// <returns>bool - has any command been executed;
    /// ConsoleCommand? - attempted command</returns>
    public async Task<(bool executed, ConsoleCommand? command)> RequestAndExecuteAsync()
    {
        var command = RequestInput();
        if (command is null)
        { return (false, null); }
        
        var result = await ExecuteCommandAsync(command);
        return (result, command);
    }
    
    public void RegisterCommand(string signature, Action<ConsoleCommand> action)
    {
        if (string.IsNullOrWhiteSpace(signature))
        { throw new ArgumentException("Command signature can't be empty"); }
        
        _availableCommands.Add(signature, action);
    }
    
    public void RegisterCommand(string signature, Func<ConsoleCommand, Task> func)
    {
        if (string.IsNullOrWhiteSpace(signature))
        { throw new ArgumentException("Command signature can't be empty"); }
        
        _asyncCommands.Add(signature, func);
    }
    
    private bool ExecuteCommand(ConsoleCommand command)
    {
        if (!_availableCommands.TryGetValue(command.Signature, out var action)) 
        {  return false; }
        action.Invoke(command);
        return true;
    }
    
    private async Task<bool> ExecuteCommandAsync(ConsoleCommand command)
    {
        if (_asyncCommands.TryGetValue(command.Signature, out var asyncAction))
        {
            await asyncAction(command);
            return true;
        }
        if (_availableCommands.TryGetValue(command.Signature, out var action))
        {
            action.Invoke(command);
            return true;
        }
        return false;
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