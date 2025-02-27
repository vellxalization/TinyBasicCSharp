using TinyBasicCSharp.Tokenization;

namespace TinyBasicCSharp.Parsing;

/// <summary>
/// Class for parsing functions
/// </summary>
public class FunctionParser
{
    private static readonly Dictionary<string, IFunctionParser> Map = new()
    {
        { "RND", new RandomParser() }
    };
    
    /// <summary>
    /// Checks if a function with provided name exists
    /// </summary>
    /// <param name="name">String to check</param>
    /// <returns>true if the function exists, false otherwise</returns>
    public static bool IsValidFunctionName(string name) => Map.ContainsKey(name);
    
    /// <summary>
    /// Returns all the function names.
    /// </summary>
    /// <returns>Array of function names.</returns>
    public static string[] GetFunctionNames() => Map.Keys.ToArray();
    
    /// <summary>
    /// Parses a span of tokens into a function token
    /// </summary>
    /// <param name="selectedTokens">Span of tokens</param>
    /// <returns>Function token if parsing is successful</returns>
    public static FunctionToken ParseFunction(Span<IToken> selectedTokens)
    {
        if (selectedTokens.Length < 3)
        { throw new ParsingException("Function should contain at least 3 tokens (signature and a pair of parentheses)"); }
        
        if (selectedTokens[0] is not WordToken signature || !Map.TryGetValue(signature.Value, out var parser))
        { throw new UnexpectedTokenException($"Expected a valid function name, got: {selectedTokens[0]}"); }
        if (selectedTokens[1] is not ServiceToken { Type: ServiceType.ParenthesisOpen })
        { throw new UnexpectedTokenException($"Expected an open parenthesis after function name {signature}"); }
        if (selectedTokens[^1] is not ServiceToken { Type: ServiceType.ParenthesisClose })
        { throw new UnexpectedTokenException($"Expected a closing parenthesis after arguments for function {signature}"); }
        
        IToken[][] arguments = [];
        if (selectedTokens.Length > 3)
        {
            var argsSlice = selectedTokens[2..^1];
            try
            { arguments = SliceArguments(argsSlice); }
            catch (ParsingException ex)
            { throw new ParsingException($"Error parsing {signature} function", ex); }
        }

        try
        { return parser.Parse(signature.Value, arguments); }
        catch (ParsingException ex)
        { throw new ParsingException($"Error parsing {signature} function", ex); }
    }
    
    private static IToken[][] SliceArguments(Span<IToken> argsSlice)
    {
        List<IToken[]> arguments = new(2);
        int anchor = 0;
        for (var i = 0; i < argsSlice.Length; ++i)
        {
            if (argsSlice[i] is not ServiceToken { Type: ServiceType.Comma })
            { continue; }

            ++i;
            if (i >= argsSlice.Length 
                || argsSlice[i] is ServiceToken { Type: ServiceType.Comma or ServiceType.Newline })
            { throw new UnexpectedTokenException("Expected next argument after comma"); }
            
            arguments.Add(argsSlice[anchor..(i - 1)].ToArray());
            anchor = i;
        }
        
        arguments.Add(argsSlice[anchor..].ToArray());
        return arguments.ToArray();
    }
    
    /// <summary>
    /// Selects a function tokens by matching a function name and everything inside parentheses
    /// </summary>
    /// <param name="tokens">Span of tokens</param>
    /// <param name="startFrom">Pointer from where function will try to match tokens</param>
    /// <returns>Span of tokens</returns>
    public static Span<IToken> SelectFunctionTokens(Span<IToken> tokens, int startFrom)
    {
        if (tokens[startFrom] is not WordToken signature || !IsValidFunctionName(signature.Value))
        { return []; }

        int pointerCopy = startFrom + 1;
        if (pointerCopy >= tokens.Length || tokens[pointerCopy] is not ServiceToken { Type: ServiceType.ParenthesisOpen })
        { return tokens.Slice(startFrom, 1); }
        
        ++pointerCopy;
        int parenthesisCount = 1;
        while (parenthesisCount > 0)
        {
            if (pointerCopy >= tokens.Length)
            { return tokens[startFrom..]; }

            if (tokens[pointerCopy] is not ServiceToken service)
            {
                ++pointerCopy;
                continue;
            }
            
            if (service.Type == ServiceType.Newline)
            { break; }
            
            if (service.Type == ServiceType.ParenthesisOpen)
            { ++parenthesisCount; }
            else if (service.Type == ServiceType.ParenthesisClose)
            { --parenthesisCount; }
            
            ++pointerCopy;
        }
        
        return tokens[startFrom..pointerCopy];
    }
}