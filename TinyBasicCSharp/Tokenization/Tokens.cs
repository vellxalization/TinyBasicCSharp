using System.Text;

namespace TinyCompilerForTinyBasic.Tokenization;

// yes, this is a marker interface. Too bad
public interface IToken
{ }

public record WordToken(string Value) : IToken
{
    public override string ToString() => Value;
}

public record NumberToken(int Value) : IToken
{
    public override string ToString() => Value.ToString();
}

public record QuotedStringToken(string Value) : IToken
{
    public override string ToString() => Value;
}

public record OperatorToken(OperatorType Type) : IToken
{
    public override string ToString()
    {
        return Type switch
        {
            OperatorType.Plus => "+",
            OperatorType.Minus => "-",
            OperatorType.Multiplication => "*",
            OperatorType.Division => "/",
            OperatorType.Equals => "=",
            OperatorType.NotEqual => "<>",
            OperatorType.GreaterThan => ">",
            OperatorType.GreaterThanOrEqual => ">=",
            OperatorType.LessThan => "<",
            OperatorType.LessThanOrEqual => "<=",
            _ => throw new ArgumentException($"Invalid operator type: {Type.ToString()}")
        };
    }
}

public enum OperatorType
{
    Plus,
    Minus,
    Multiplication,
    Division,
    Equals,
    NotEqual,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
}

public record ServiceToken(ServiceType Type) : IToken
{
    public override string ToString()
    {
        return Type switch
        {
            ServiceType.Newline => System.Environment.NewLine,
            ServiceType.Comma => ",",
            ServiceType.ParenthesisOpen => "(",
            ServiceType.ParenthesisClose => ")",
            _ => throw new ArgumentException($"Invalid service token: {Type.ToString()}")
        };
    }
}

public enum ServiceType
{
    Newline,
    Comma,
    ParenthesisOpen,
    ParenthesisClose
}

public record FunctionToken(string Signature, IToken[] Arguments) : IToken
{
    public override string ToString()
    {
        var builder = new StringBuilder(Signature);
        builder.Append('(');
        foreach(var arg in Arguments)
        { builder.Append(arg); }

        builder.Append(')');
        return builder.ToString();
    }
}

public record ExpressionToken(IToken[] Arguments) : IToken
{
    public override string ToString()
    {
        var builder = new StringBuilder();
        foreach (var arg in Arguments)
        {
            if (arg is not ServiceToken { Type: ServiceType.ParenthesisOpen or ServiceType.ParenthesisClose } parenthesis)
            {
                builder.Append(arg);
                builder.Append(' '); 
                continue;
            }

            if (parenthesis.Type == ServiceType.ParenthesisOpen)
            { builder.Append(arg); }
            else // closing parenthesis
            {
                builder.Insert(builder.Length - 1, arg); // replace space with ')'
                builder.Append(' ');
            } 
        }
        
        return builder.ToString()[..^1];
    }
}

public record Statement(StatementType Type, IToken[] Arguments, short? Label) : IToken
{
    public override string ToString()
    {
        var builder = new StringBuilder();
        if (Label != null)
        {
            builder.Append(Label);
            builder.Append(' ');
        }
        
        builder.Append(Type.ToString().ToUpper());
        if (Arguments.Length == 0)
        { return builder.ToString(); }

        builder.Append(' ');
        foreach (var arg in Arguments)
        {
            builder.Append(arg);
            builder.Append(' ');
        }
        
        
        { return builder.ToString()[..^1]; }
    }
};

public enum StatementType
{
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