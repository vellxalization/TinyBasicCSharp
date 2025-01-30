using TinyCompilerForTinyBasic.Tokenization;

namespace TinyCompilerForTinyBasic.Parsing;

public static class Parser
{
    private static Dictionary<string, IStatementParser>? _map = null;
    
    public static Statement ParseStatement(Span<IToken> singleLine)
    {
        if (singleLine.Length == 0 || singleLine[0] is ServiceToken { Type: ServiceType.Newline })
        { return new Statement(StatementType.Newline, [], null); }
        
        var label = TryParseLabel(singleLine[0]);
        var statementIndex = label == null ? 0 : 1;
        if (label != null && (singleLine.Length == 1 || singleLine[1] is ServiceToken { Type:ServiceType.Newline }))
        { return new Statement(StatementType.Newline, [], label); }
        
        if (_map == null)
        { InitMap(); }
        
        if (!_map!.TryGetValue(singleLine[statementIndex].ToString()!, out var parser))
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

    private static void InitMap()
    {
        var jump = new JumpParser();
        var single = new SingleWordParser();
        _map = new Dictionary<string, IStatementParser>()
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
}