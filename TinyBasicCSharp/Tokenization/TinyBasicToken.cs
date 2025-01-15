using System.Data;
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

    public override string ToString() => Type == TokenType.QuotedString ? $"\"{Value}\"" : Value;
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
        { builder.Append(token); }
        
        return builder.ToString();
    }
}

public class FunctionToken : TinyBasicToken
{
    public FunctionToken(TinyBasicToken[] arguments, string signature) : base(TokenType.Function)
    {
        Arguments = arguments;
        Signature = signature;
    }
    
    public string Signature { get; init; }
    public TinyBasicToken[] Arguments { get; init; }

    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.Append(Signature);
        builder.Append('(');
        foreach (var arg in Arguments)
        {
            builder.Append(arg);
            builder.Append(',');
        }
        if (Arguments.Length > 0)
        { builder.Remove(builder.Length - 1, 1); }
        builder.Append(')');
        
        return builder.ToString();
    }
}

public class Statement : TinyBasicToken
{
    public short? Label { get; init; } = null;
    public StatementType StatementType { get; init; } = StatementType.Unknown;
    public TinyBasicToken[] Arguments { get; init; } = [];

    public Statement(short? label, StatementType statementType, TinyBasicToken[] arguments) : this()
    {
        Label = label;
        StatementType = statementType;
        Arguments = arguments;
    }
    
    public Statement() : base(TokenType.Statement) {}

    public override string ToString()
    {
        var builder = new StringBuilder();
        if (Label is not null)
        {
            builder.Append(Label);
            builder.Append(' ');
        }

        builder.Append(StatementType.ToString().ToUpper());
        builder.Append(' ');
        foreach (var arg in Arguments)
        {
            builder.Append(arg);
            builder.Append(' ');
        }
        if (Arguments.Length > 0)
        { builder.Remove(builder.Length - 1, 1); }
        
        return builder.ToString();
    }
}

public enum StatementType
{
    Unknown,
    Newline,
    Print,
    Let,
    If,
    Goto,
    Gosub,
    Input,
    Return,
    Clear,
    List,
    Run,
    End,
    Rem
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
    Expression,
    Function,
    Statement
}