using System.Text;

namespace TinyCompilerForTinyBasic.Tokenization;

/// <summary>
/// A TinyBasic token. Base class for ValueTinyBasicToken and ExpressionTinyBasicToken.
/// </summary>
public class TinyBasicToken
{
    public TinyBasicToken(){}
    public TinyBasicToken(TokenType type) => Type = type;
    
    public TokenType Type { get; init; } = TokenType.Unknown;

    public override string ToString()
    {
        switch (Type)
        {
            case TokenType.Unknown:
            { return "UNKNOWN"; }
            case TokenType.Comma:
            { return ","; }
            case TokenType.NewLine:
            { return "\n"; }
            case TokenType.OperatorPlus:
            { return "+"; }
            case TokenType.OperatorMinus:
            { return "-"; }
            case TokenType.OperatorMultiplication:
            { return "*"; }
            case TokenType.OperatorDivision:
            { return "/"; }
            case TokenType.OperatorGreaterThan:
            { return ">"; }
            case TokenType.OperatorGreaterThanOrEqual:
            { return ">="; }
            case TokenType.OperatorLessThan:
            { return "<"; }
            case TokenType.OperatorLessThanOrEqual:
            { return "<="; }
            case TokenType.OperatorEquals:
            { return "="; }
            case TokenType.OperatorNotEqual:
            { return "<>"; }
            case TokenType.ParenthesisOpen:
            { return "("; }
            case TokenType.ParenthesisClose:
            { return ")"; }
            default:
            { return base.ToString()!; }
        }
    }
}

/// <summary>
/// TinyBasic token for storing variable values, such as strings and numbers
/// </summary>
public class ValueToken : TinyBasicToken
{
    public ValueToken(){}
    public ValueToken(TokenType type, string value) : base(type) => Value = value;
    
    public string Value { get; init; } = string.Empty;

    public override string ToString() => Value;
}

/// <summary>
/// TinyBasic token for storing expressions (a sequence of numbers, operators and variables)
/// </summary>
public class ExpressionToken : TinyBasicToken
{
    public ExpressionToken() : base(TokenType.Expression) { }
    public ExpressionToken(TinyBasicToken[] tokens) : this() => Components = tokens;
    public TinyBasicToken[] Components { get; init; } = [];

    public override string ToString()
    {
        var builder = new StringBuilder();

        foreach (TinyBasicToken token in Components)
        {
            // if (token.Type is TBTokenType.ParenthesisClose) // remove space before ')'
            // { builder.Remove(builder.Length - 1, 1); }
            
            builder.Append(token);
            
            // if (token.Type is not TBTokenType.ParenthesisOpen) // not adding space after '('
            // { builder.Append(' '); }
        }
        
        // builder.Remove(builder.Length - 1, 1);
        return builder.ToString();
    }
}

public enum TokenType
{
    Unknown,
    Comma,
    NewLine,
    Number,
    String,
    QuotedString,
    ParenthesisOpen,
    ParenthesisClose,
    OperatorPlus,
    OperatorMinus,
    OperatorMultiplication,
    OperatorDivision,
    OperatorEquals,
    OperatorNotEqual,
    OperatorGreaterThan,
    OperatorGreaterThanOrEqual,
    OperatorLessThan,
    OperatorLessThanOrEqual,
    Expression
}