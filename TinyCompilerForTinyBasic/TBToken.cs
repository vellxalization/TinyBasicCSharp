namespace TinyCompilerForTinyBasic;

public class TBToken
{
    public TBToken()
    {}
    
    public TBToken(TBTokenType type, string? value)
    {
        Type = type;
        Value = value;
    }
    
    public TBTokenType Type { get; init; }
    public string? Value { get; init; }
}

public enum TBTokenType
{
    Unknown,
    Keyword,
    Operator,
    Parenthesis,
    Variable,
    Number,
    String,
}