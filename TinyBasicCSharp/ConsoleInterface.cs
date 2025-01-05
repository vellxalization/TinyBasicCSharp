using System.Text.RegularExpressions;

namespace TinyCompilerForTinyBasic;

public class ConsoleInterface<T>
{
    public string? InputRequestPrefix { get; init; } = null;
    public Action<T, ConsoleCommand>? Fallback { get; init; }
    private readonly Dictionary<string, Action<T, ConsoleCommand>> _availableCommands = new();
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
    
    public void RegisterCommand(string signature, Action<T, ConsoleCommand> action)
    {
        if (string.IsNullOrWhiteSpace(signature))
        { throw new ArgumentException("Command signature can't be empty"); }
        
        _availableCommands.Add(signature, action);
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