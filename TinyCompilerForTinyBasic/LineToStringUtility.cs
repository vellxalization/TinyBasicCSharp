using System.Text;

namespace TinyCompilerForTinyBasic;

public static class LineToStringUtility
{
    public static string LineToString(IEnumerable<TinyBasicToken> line)
    {
        var builder = new StringBuilder();

        foreach (TinyBasicToken token in line)
        {
            if (token.Type is TBTokenType.ParenthesisClose) // remove space before ')'
            { builder.Remove(builder.Length - 1, 1); }
            
            builder.Append(TokenToString(token));
            
            if (token.Type is not TBTokenType.ParenthesisOpen) // not adding space after ')'
            { builder.Append(' '); }
        }
        
        return builder.ToString();
    }
    
    public static string LineToString(Span<TinyBasicToken> line)
    {
        var builder = new StringBuilder();

        foreach (TinyBasicToken token in line)
        {
            if (token.Type is TBTokenType.ParenthesisClose) // remove space before ')'
            { builder.Remove(builder.Length - 1, 1); }
            
            builder.Append(TokenToString(token));
            
            if (token.Type is not TBTokenType.ParenthesisOpen) // not adding space after ')'
            { builder.Append(' '); }
        }
        
        return builder.ToString();
    }

    public static string TokenToString(TinyBasicToken token)
    {
        return token.Type switch
        {
            TBTokenType.Unknown => "UNKNOWN",
            TBTokenType.Comma => ",",
            TBTokenType.NewLine => "\n",
            TBTokenType.OperatorPlus => "+",
            TBTokenType.OperatorMinus => "-",
            TBTokenType.OperatorMultiplication => "*",
            TBTokenType.OperatorDivision => "/",
            TBTokenType.OperatorGreaterThan => ">",
            TBTokenType.OperatorGreaterThanOrEqual => ">=",
            TBTokenType.OperatorLessThan => "<",
            TBTokenType.OperatorLessThanOrEqual => "<=",
            TBTokenType.OperatorEquals => "=",
            TBTokenType.OperatorNotEqual => "<>",
            TBTokenType.ParenthesisOpen => "(",
            TBTokenType.ParenthesisClose => ")",
            _ => ValueTokenToString((ValueTinyBasicTinyBasicToken)token)
        };
    }

    private static string ValueTokenToString(ValueTinyBasicTinyBasicToken token) => token.Value;
}