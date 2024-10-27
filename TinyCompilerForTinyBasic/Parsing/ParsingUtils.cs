using TinyCompilerForTinyBasic.Tokenization;

namespace TinyCompilerForTinyBasic.Parsing;

public static class ParsingUtils
{
    public static ExpressionTinyBasicToken SelectExpressionFromLine(TinyBasicToken[] line, ref int start)
    {
        int pointerCopy = start;

        while ((start + 1) < line.Length)
        {
            TinyBasicToken token = line[start + 1];
            if (token.Type is TBTokenType.String)
            {
                if (!char.TryParse(token.ToString(), out _))
                { break; }
            }
            else if (token.Type is not (TBTokenType.ParenthesisClose or TBTokenType.ParenthesisOpen or
                TBTokenType.OperatorPlus or TBTokenType.OperatorMinus or
                TBTokenType.OperatorDivision or TBTokenType.OperatorMultiplication or
                TBTokenType.Number))
            { break; }
            
            ++start;
        }
        return new ExpressionTinyBasicToken(line[pointerCopy..(start + 1)]);
    }
    
    
    public static void ParseExpression(ExpressionTinyBasicToken expressionToken)
    {
        if (expressionToken.Components.Length < 1)
        { throw new ParsingException("Tried to parse an empty expression"); }
        
        int start = 0;
        ParseExpression(expressionToken, ref start);
    }
    
    private static void ParseExpression(ExpressionTinyBasicToken expressionToken, ref int start)
    {
        TinyBasicToken[] expression = expressionToken.Components;
        TinyBasicToken token = expression[start];
        if (token.Type is TBTokenType.OperatorPlus or TBTokenType.OperatorMinus)
        {
            ++start;
            if (start >= expression.Length)
            { throw new ParsingException($"Expected a term after unary operator \"{token}\""); }
        }
        ParseTerm(expressionToken, ref start);
        
        while ((start + 1) < expression.Length)
        {
            token = expression[start + 1];
            if (token.Type is not (TBTokenType.OperatorPlus or TBTokenType.OperatorMinus))
            { return; } 
            
            // continue parsing term only if next operator is + or -
            start += 2;
            if (start >= expression.Length)
            { throw new ParsingException($"Expected a term after \"{token}\" operator in \"{expressionToken}\" expression"); }
            
            ParseTerm(expressionToken, ref start);
        }
    }

    private static void ParseTerm(ExpressionTinyBasicToken expressionToken, ref int start)
    {
        ParseFactor(expressionToken, ref start);
        
        TinyBasicToken[] expression = expressionToken.Components;
        while ((start + 1) < expression.Length)
        {
            TinyBasicToken token = expression[start + 1];
            if (token.Type is not (TBTokenType.OperatorMultiplication or TBTokenType.OperatorDivision))
            { return; } 
            
            // continue parsing term only if next operator is * or /
            start += 2;
            if (start >= expression.Length)
            { throw new ParsingException($"Expected a term after \"{token}\" operator in \"{expressionToken}\" expression"); }
            
            ParseFactor(expressionToken, ref start);
        }
    }
    
    private static void ParseFactor(ExpressionTinyBasicToken expressionToken, ref int start)
    {
        TinyBasicToken[] expression = expressionToken.Components;
        TinyBasicToken token = expression[start];
        switch (token.Type)
        {
            case TBTokenType.Number:
            { return; }
            case TBTokenType.ParenthesisOpen:
            {
                ++start;
                ParseExpression(expressionToken, ref start);
                ++start;
                if ((start >= expression.Length) || (expression[start].Type is not TBTokenType.ParenthesisClose))
                { throw new ParsingException($"Expected a closing parenthesis after expression in \"{expressionToken}\" expression"); }
                
                return;
            }
            case TBTokenType.String:
            {
                if ((!char.TryParse(token.ToString(), out char address)) || (address is < 'A' or > 'Z'))
                { throw new ParsingException($"Expected a valid variable name (\"{token}\") in \"{expressionToken}\" expression"); }

                return;
            }
            default:
            { throw new ParsingException($"Unexpected token (\"{token}\") while parsing factor in \"{expressionToken}\" expression"); }
        }
    }
}