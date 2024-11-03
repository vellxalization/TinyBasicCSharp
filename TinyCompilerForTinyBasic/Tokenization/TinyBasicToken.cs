using System.Text;

namespace TinyCompilerForTinyBasic.Tokenization;

/// <summary>
/// A TinyBasic token. Base class for ValueTinyBasicToken and ExpressionTinyBasicToken.
/// </summary>
public class TinyBasicToken
{
    public TinyBasicToken(){}
    public TinyBasicToken(TBTokenType type) => Type = type;
    
    public TBTokenType Type { get; init; } = TBTokenType.Unknown;

    public override string ToString()
    {
        switch (Type)
        {
            case TBTokenType.Unknown:
            { return "UNKNOWN"; }
            case TBTokenType.Comma:
            { return ","; }
            case TBTokenType.NewLine:
            { return "\n"; }
            case TBTokenType.OperatorPlus:
            { return "+"; }
            case TBTokenType.OperatorMinus:
            { return "-"; }
            case TBTokenType.OperatorMultiplication:
            { return "*"; }
            case TBTokenType.OperatorDivision:
            { return "/"; }
            case TBTokenType.OperatorGreaterThan:
            { return ">"; }
            case TBTokenType.OperatorGreaterThanOrEqual:
            { return ">="; }
            case TBTokenType.OperatorLessThan:
            { return "<"; }
            case TBTokenType.OperatorLessThanOrEqual:
            { return "<="; }
            case TBTokenType.OperatorEquals:
            { return "="; }
            case TBTokenType.OperatorNotEqual:
            { return "<>"; }
            case TBTokenType.ParenthesisOpen:
            { return "("; }
            case TBTokenType.ParenthesisClose:
            { return ")"; }
            default:
            { return base.ToString()!; }
        }
    }
}

/// <summary>
/// TinyBasic token for storing variable values, such as strings and numbers
/// </summary>
public class ValueTinyBasicToken : TinyBasicToken
{
    public ValueTinyBasicToken(){}
    public ValueTinyBasicToken(TBTokenType type, string value) : base(type) => Value = value;
    
    public string Value { get; init; } = string.Empty;

    public override string ToString() => Value;
}

/// <summary>
/// TinyBasic token for storing expressions (a sequence of numbers, operators and variables)
/// </summary>
public class ExpressionTinyBasicToken : TinyBasicToken
{
    public ExpressionTinyBasicToken() : base(TBTokenType.Expression) { }
    public ExpressionTinyBasicToken(TinyBasicToken[] tokens) : this() => Components = tokens;
    public TinyBasicToken[] Components { get; init; } = [];

    public override string ToString()
    {
        var builder = new StringBuilder();

        foreach (TinyBasicToken token in Components)
        {
            if (token.Type is TBTokenType.ParenthesisClose) // remove space before ')'
            { builder.Remove(builder.Length - 1, 1); }
            
            builder.Append(token);
            
            if (token.Type is not TBTokenType.ParenthesisOpen) // not adding space after '('
            { builder.Append(' '); }
        }
        
        builder.Remove(builder.Length - 1, 1);
        return builder.ToString();
    }
}

public enum TBTokenType
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