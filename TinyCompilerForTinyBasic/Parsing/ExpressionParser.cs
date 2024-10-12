namespace TinyCompilerForTinyBasic.Parsing;

public static class ExpressionParser
{
    public static void ParseExpression(Span<TinyBasicToken> expression)
    {
        if (expression.Length < 1)
        { throw new ParsingException("Tried to parse an empty expression"); }
        
        int start = 0;
        ParseExpression(expression, ref start);
    }

    private static void ParseExpression(Span<TinyBasicToken> expression, ref int start)
    {
        TinyBasicToken token = expression[start];
        if (token.Type is TBTokenType.OperatorPlus or TBTokenType.OperatorMinus)
        {
            ++start;
            if (start >= expression.Length)
            { throw new ParsingException($"Expected a term after: unary {LineToStringUtility.TokenToString(token)} operator"); }
        }
        ParseTerm(expression, ref start);
        
        while ((start + 1) < expression.Length)
        {
            token = expression[start + 1];
            if (token.Type is not (TBTokenType.OperatorPlus or TBTokenType.OperatorMinus))
            { return; } 
            
            // continue parsing term only if next operator is + or -
            start += 2;
            if (start >= expression.Length)
            { throw new ParsingException($"Expected a term after: {LineToStringUtility.TokenToString(token)} operator @ {LineToStringUtility.LineToString(expression)}"); }
            
            ParseTerm(expression, ref start);
        }
    }

    private static void ParseTerm(Span<TinyBasicToken> expression, ref int start)
    {
        ParseFactor(expression, ref start);
        
        while ((start + 1) < expression.Length)
        {
            TinyBasicToken token = expression[start + 1];
            if (token.Type is not (TBTokenType.OperatorMultiplication or TBTokenType.OperatorDivision))
            { return; } 
            
            // continue parsing term only if next operator is * or /
            start += 2;
            if (start >= expression.Length)
            { throw new ParsingException($"Expected a term after: {LineToStringUtility.TokenToString(token)} operator @ {LineToStringUtility.LineToString(expression)}"); }
            
            ParseFactor(expression, ref start);
        }
    }
    
    private static void ParseFactor(Span<TinyBasicToken> expression, ref int start)
    {
        TinyBasicToken token = expression[start];
        switch (token.Type)
        {
            case TBTokenType.Number:
            { return; }
            case TBTokenType.ParenthesisOpen:
            {
                ++start;
                ParseExpression(expression, ref start);
                if (((start + 1) >= expression.Length) || (expression[start + 1].Type is not TBTokenType.ParenthesisClose))
                { throw new ParsingException($"Expected a closing parenthesis after expression @ {LineToStringUtility.LineToString(expression)}"); }
                ++start;
                
                return;
            }
            case TBTokenType.String:
            {
                string value = ((ValueTinyBasicTinyBasicToken)token).Value;
                if ((!char.TryParse(value, out char address)) || (address is < 'A' or > 'Z'))
                { throw new ParsingException($"Expected a valid variable name: {value} @ {LineToStringUtility.LineToString(expression)}"); }

                return;
            }
            default:
            { throw new ParsingException($"Unexpected token while parsing factor: {LineToStringUtility.TokenToString(token)} @ {LineToStringUtility.LineToString(expression)}"); }
        }
    }
}