using System.Text;
using TinyCompilerForTinyBasic.Tokenization;

namespace TinyCompilerForTinyBasic.Parsing;

public class ExpressionParser
{
    /// <summary>
    /// Matches all possible expression tokens (operators, variables, numbers, function calls) and returns span of them.
    /// </summary>
    /// <param name="line">Array of tokens</param>
    /// <param name="start">Reference to the int pointer from where method will try to select the expression.
    /// Will be incremented in the process</param>
    /// <returns>Span of the original array</returns>
    public static Span<TinyBasicToken> SelectExpressionFromLine(TinyBasicToken[] line, int start)
    {
        if (!IsValidExpressionToken(line[start]))
        { return []; }

        int pointerCopy = start;
        while ((start + 1) < line.Length)
        {
            TinyBasicToken token = line[start + 1];
            if (!IsValidExpressionToken(token))
            { break; }
            
            ++start;
        }
        return line.AsSpan(pointerCopy, start - pointerCopy + 1);
    }

    private static bool IsValidExpressionToken(TinyBasicToken token)
    {
        if (token.Type is TokenType.String)
        {
            string value = token.ToString();
            if (value is "RND")
            { return true; }
            
            if (!char.TryParse(value, out _))
            { return false; }
        }
        else if (token.Type is not (TokenType.ParenthesisClose or TokenType.ParenthesisOpen or
                 TokenType.OperatorPlus or TokenType.OperatorMinus or
                 TokenType.OperatorDivision or TokenType.OperatorMultiplication or
                 TokenType.Number))
        { return false; }

        return true;
    }

    public static ExpressionToken ParseExpression(Span<TinyBasicToken> selectedTokens)
    {
        if (selectedTokens.Length is 0)
        { throw new EmptyExpressionException("Tried to parse an empty expression"); }
        
        List<TinyBasicToken> expression = [];
        int start = 0;
        try
        { ParseExpression(selectedTokens, ref start, expression); }
        catch (ParsingException ex)
        { throw new ParsingException($"Error parsing expression:\n >{ex.Message}"); }
        
        if (start + 1 < selectedTokens.Length)
        { throw new UnexpectedOrEmptyTokenException($"Unexpected token {selectedTokens[start]} at the end of expression {ExpressionToString(expression)}"); }
        
        return new ExpressionToken(expression.ToArray());
    }

    private static void ParseExpression(Span<TinyBasicToken> selectedTokens, ref int start, List<TinyBasicToken> finalExpression)
    {
        ParseTerm(selectedTokens, ref start, finalExpression);
        
        while (true)
        {
            if (start + 1 >= selectedTokens.Length)
            { return; }
            
            var op = selectedTokens[start + 1];
            if (op.Type is not (TokenType.OperatorPlus or TokenType.OperatorMinus))
            { return; }
            finalExpression.Add(op);
            
            ++start;
            if (start + 1 >= selectedTokens.Length)
            { throw new UnexpectedOrEmptyTokenException($"Expected a factor after operator: {ExpressionToString(finalExpression)}"); }
            
            ++start;
            ParseTerm(selectedTokens, ref start, finalExpression);
        }
    }

    private static void ParseTerm(Span<TinyBasicToken> selectedTokens, ref int start, List<TinyBasicToken> finalExpression)
    {
        ParseFactor(selectedTokens, ref start, finalExpression);
        
        while (true)
        {
            if (start + 1 >= selectedTokens.Length)
            { return; }
            
            var op = selectedTokens[start + 1];
            if (op.Type is not (TokenType.OperatorDivision or TokenType.OperatorMultiplication))
            { return; }
            finalExpression.Add(op);
            
            ++start;
            if (start + 1 >= selectedTokens.Length)
            { throw new UnexpectedOrEmptyTokenException($"Expected a factor after operator: {ExpressionToString(finalExpression)}"); }
            
            ++start;
            ParseFactor(selectedTokens, ref start, finalExpression);
        }
    }
    
    private static void ParseFactor(Span<TinyBasicToken> selectedTokens, ref int start, List<TinyBasicToken> finalExpression)
    {
        var token = selectedTokens[start];
        while (token.Type is TokenType.OperatorPlus or TokenType.OperatorMinus)
        {
            finalExpression.Add(token);
            ++start;
            if (start >= selectedTokens.Length)
            { throw new UnexpectedOrEmptyTokenException($"Unmatched unary operator: {ExpressionToString(finalExpression)}"); }

            token = selectedTokens[start];
        }

        switch (token.Type)
        {
            case TokenType.Number:
            {
                finalExpression.Add(token);
                return;
            }
            case TokenType.ParenthesisOpen:
            {
                finalExpression.Add(token);
                ++start;
                ParseExpression(selectedTokens, ref start, finalExpression);
                ++start;
                if (start >= selectedTokens.Length || selectedTokens[start].Type != TokenType.ParenthesisClose)
                { throw new UnexpectedOrEmptyTokenException($"Expected a closing parenthesis: {ExpressionToString(finalExpression)}"); }
                finalExpression.Add(selectedTokens[start]);

                return;
            }
            case TokenType.String:
            {
                var value = token.ToString();
                if (char.TryParse(value, out char address) && address is >= 'A' and <= 'Z')
                {
                    finalExpression.Add(token);
                    return;
                }

                if (value is not "RND")
                { throw new UnexpectedOrEmptyTokenException($"Got string {value}; expected a valid variable address or function call"); }
                
                var functionSpan = FunctionParser.SelectFunctionTokens(selectedTokens.ToArray(), start);
                try
                {
                    var functionToken = FunctionParser.ParseFunction(functionSpan);
                    finalExpression.Add(functionToken);
                }
                catch (ParsingException ex)
                { throw new ParsingException($"Error while parsing function in expression:\n >{ex.Message}"); }

                start += functionSpan.Length;
                return;
            }
            default:
            { throw new UnexpectedOrEmptyTokenException($"Got unexpected token {token}"); }
        }
    }

    private static string ExpressionToString(IEnumerable<TinyBasicToken> expression)
    {
        var sb = new StringBuilder();
        foreach (var token in expression)
        { sb.Append(token); }
        
        return sb.ToString();
    }
}