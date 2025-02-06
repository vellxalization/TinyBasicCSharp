using TinyBasicCSharp.Tokenization;

namespace TinyBasicCSharp.Parsing;

public static class Parser
{
    private static Lazy<Dictionary<string, IStatementParser>> _map = new(CreateMap);
    
    public static Statement ParseStatement(Span<IToken> singleLine)
    {
        if (singleLine.Length == 0 || singleLine[0] is ServiceToken { Type: ServiceType.Newline })
        { return new Statement(StatementType.Newline, [], null); }
        
        var label = TryParseLabel(singleLine[0]);
        var statementIndex = label == null ? 0 : 1;
        if (label != null && (singleLine.Length == 1 || singleLine[1] is ServiceToken { Type:ServiceType.Newline }))
        { return new Statement(StatementType.Newline, [], label); }
        
        if (!_map.Value.TryGetValue(singleLine[statementIndex].ToString()!, out var parser))
        { throw new UnexpectedTokenException($"Unrecognized keyword: {singleLine[statementIndex]}"); }
        
        try
        {
            var statement = parser.Parse(singleLine[statementIndex..]);
            return statement with { Label = label };
        }
        catch(ParsingException ex)
        { throw new ParsingException($"Error parsing {singleLine[statementIndex]} statement", ex); }
    }
    
    private static short? TryParseLabel(IToken token)
    {
        if (token is not NumberToken numberToken)
        { return null; }
        
        if (numberToken.Value is < 0 or > short.MaxValue)
        { throw new InvalidLabelException(numberToken.Value); }
        
        return (short)numberToken.Value;
    }

    private static Dictionary<string, IStatementParser> CreateMap()
    {
        var jump = new JumpParser();
        var single = new SingleWordParser();
        var map = new Dictionary<string, IStatementParser>()
        {
            { "LET", new LetParser() },
            { "IF", new IfParser() },
            { "PRINT", new PrintParser() },
            { "INPUT", new InputParser() },
            { "GOTO", jump },   
            { "GOSUB", jump }, 
            { "END", single },
            { "RETURN", single },
            { "CLEAR", single },
            { "LIST", single },
            { "RUN", single },
            { "REM", new RemParser() }
        };
        return map;
    }
    
    public static IToken[][] SplitByNewline(IToken[] tokens)
    {
        var anchor = 0;
        var lines = new List<IToken[]>();
        for (int i = 0; i < tokens.Length; ++i)
        {
            var token = tokens[i];
            if (token is not ServiceToken { Type: ServiceType.Newline })
            { continue; }
            
            lines.Add(tokens[anchor..(i + 1)]);
            anchor = i + 1;
        }
        
        lines.Add(tokens[anchor..]);
        return lines.ToArray();
    }
    
    public static string[] GetAllStatements() => _map.Value.Keys.ToArray();
}