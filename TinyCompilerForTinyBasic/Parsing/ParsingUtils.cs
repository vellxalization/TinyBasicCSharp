namespace TinyCompilerForTinyBasic.Parsing;

public static class ParsingUtils
{
    public static ExpressionTinyBasicToken SelectExpressionFromLine(TinyBasicToken[] line, ref int start)
    {
        int pointerCopy = start;

        while (((start + 1) < line.Length) && (line[start + 1].Type is TBTokenType.ParenthesisClose
                   or TBTokenType.ParenthesisOpen or
                   TBTokenType.OperatorPlus or TBTokenType.OperatorMinus or
                   TBTokenType.OperatorDivision or TBTokenType.OperatorMultiplication or
                   TBTokenType.Number or TBTokenType.String))
        {
            TinyBasicToken token = line[start + 1];
            if (token.Type is TBTokenType.String)
            {
                if (!char.TryParse(token.ToString(), out _))
                { break; }
            }
            ++start;
        }
        return new ExpressionTinyBasicToken(line[pointerCopy..(start + 1)]);
    }
    
    
    public static void ParseExpression(ExpressionTinyBasicToken expression)
    {
        if (expression.Components.Length < 1)
        { throw new ParsingException("Tried to parse an empty expression"); }
        
        int start = 0;
        ParseExpression(expression.Components, ref start);
    }
    
    private static void ParseExpression(TinyBasicToken[] expression, ref int start)
    {
        TinyBasicToken token = expression[start];
        if (token.Type is TBTokenType.OperatorPlus or TBTokenType.OperatorMinus)
        {
            ++start;
            if (start >= expression.Length)
            { throw new ParsingException($"Expected a term after unary {token} operator"); }
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
            { throw new ParsingException($"Expected a term after: {token} operator"); }
            
            ParseTerm(expression, ref start);
        }
    }

    private static void ParseTerm(TinyBasicToken[] expression, ref int start)
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
            { throw new ParsingException($"Expected a term after: {token} operator"); }
            
            ParseFactor(expression, ref start);
        }
    }
    
    private static void ParseFactor(TinyBasicToken[] expression, ref int start)
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
                { throw new ParsingException("Expected a closing parenthesis after expression"); }
                ++start;
                
                return;
            }
            case TBTokenType.String:
            {
                if ((!char.TryParse(token.ToString(), out char address)) || (address is < 'A' or > 'Z'))
                { throw new ParsingException($"Expected a valid variable name: {token}"); }

                return;
            }
            default:
            { throw new ParsingException($"Unexpected token while parsing factor: {token}"); }
        }
    }
}