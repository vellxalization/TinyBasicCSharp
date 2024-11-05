using TinyCompilerForTinyBasic.Tokenization;

namespace TinyCompilerForTinyBasic.Parsing;

/// <summary>
/// Collection of helper methods used for parsing tokens
/// </summary>
public static class ParsingUtils
{
    /// <summary>
    /// Matches all possible expression tokens (operators, variables, numbers) and combines them into a single token
    /// </summary>
    /// <param name="line">Array of tokens</param>
    /// <param name="start">Reference to the int pointer from where method will try to select the expression.
    /// Will be incremented in the process</param>
    /// <returns>Expression token containing collection of tokens (if any found)</returns>
    public static ExpressionTinyBasicToken SelectExpressionFromLine(TinyBasicToken[] line, ref int start)
    {
        if (!IsValidExpressionToken(line[start]))
        { return new ExpressionTinyBasicToken(); }

        int pointerCopy = start;
        while ((start + 1) < line.Length)
        {
            TinyBasicToken token = line[start + 1];
            if (!IsValidExpressionToken(token))
            { break; }
            
            ++start;
        }
        return new ExpressionTinyBasicToken(line[pointerCopy..(start + 1)]);
    }

    private static bool IsValidExpressionToken(TinyBasicToken token)
    {
        if (token.Type is TBTokenType.String)
        {
            if (!char.TryParse(token.ToString(), out _))
            { return false; }
        }
        else if (token.Type is not (TBTokenType.ParenthesisClose or TBTokenType.ParenthesisOpen or
                 TBTokenType.OperatorPlus or TBTokenType.OperatorMinus or
                 TBTokenType.OperatorDivision or TBTokenType.OperatorMultiplication or
                 TBTokenType.Number))
        { return false; }

        return true;
    }
    
    /// <summary>
    /// Checks if the expression is syntactically correct. Returns if true, otherwise will throw exception
    /// </summary>
    /// <param name="expressionToken">Expression to check</param>
    /// <exception cref="EmptyExpressionException">Expression contains no tokens</exception>
    /// <exception cref="UnexpectedOrEmptyTokenException">Got no token or an unexpected one. Check error message for more details</exception>
    /// <exception cref="InvalidVariableNameException">Got an invalid character while reading variable name</exception>
    public static void ParseExpression(ExpressionTinyBasicToken expressionToken)
    {
        if (expressionToken.Components.Length < 1)
        { throw new EmptyExpressionException("Tried to parse an empty expression"); }
        
        int start = 0;
        ParseExpression(expressionToken, ref start);
        if (start < (expressionToken.Components.Length - 1))
        { throw new UnexpectedOrEmptyTokenException($"Unexpected token ('{expressionToken.Components[start]}') at the end of expression"); }
    }
    
    private static void ParseExpression(ExpressionTinyBasicToken expressionToken, ref int start)
    {
        TinyBasicToken[] expression = expressionToken.Components;
        ParseTerm(expressionToken, ref start);
        
        while ((start + 1) < expression.Length)
        {
            TinyBasicToken token = expression[start + 1];
            if (token.Type is not (TBTokenType.OperatorPlus or TBTokenType.OperatorMinus))
            { return; } 
            
            // continue parsing term only if next operator is + or -
            start += 2;
            if (start >= expression.Length)
            { throw new UnexpectedOrEmptyTokenException($"Expected a term after \"{token}\" operator in \"{expressionToken}\" expression"); }
            
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
            { throw new UnexpectedOrEmptyTokenException($"Expected a term after \"{token}\" operator in \"{expressionToken}\" expression"); }
            
            ParseFactor(expressionToken, ref start);
        }
    }
    
    private static void ParseFactor(ExpressionTinyBasicToken expressionToken, ref int start)
    {
        TinyBasicToken[] expression = expressionToken.Components;
        res:
        TinyBasicToken token = expression[start];
        switch (token.Type)
        {
            case (TBTokenType.OperatorPlus or TBTokenType.OperatorMinus):
            {
                ++start;
                if (start >= expression.Length)
                { throw new UnexpectedOrEmptyTokenException($"Unary operator with no number or variable in \"{expressionToken}\" expression"); }
                goto res;
            }
            case TBTokenType.Number:
            { return; }
            case TBTokenType.ParenthesisOpen:
            {
                ++start;
                ParseExpression(expressionToken, ref start);
                ++start;
                if ((start >= expression.Length) || (expression[start].Type is not TBTokenType.ParenthesisClose))
                { throw new UnexpectedOrEmptyTokenException($"Expected a closing parenthesis after expression in \"{expressionToken}\" expression"); }
                
                return;
            }
            case TBTokenType.String:
            {
                if ((!char.TryParse(token.ToString(), out char address)) || (address is < 'A' or > 'Z'))
                { throw new InvalidVariableNameException($"Expected a valid variable name (\"{token}\") in \"{expressionToken}\" expression"); }

                return;
            }
            default:
            { throw new UnexpectedOrEmptyTokenException($"Unexpected token (\"{token}\") while parsing factor in \"{expressionToken}\" expression"); }
        }
    }
}