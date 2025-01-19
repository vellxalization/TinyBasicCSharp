using System.Text;
using TinyCompilerForTinyBasic.Tokenization;

namespace TinyCompilerForTinyBasic.Parsing;

public class ExpressionParser
{
    /// <summary>
    /// Matches all possible expression tokens (operators, variables, numbers, function calls) and returns span of them.
    /// </summary>
    /// <param name="line">Array of tokens</param>
    /// <param name="start">Reference to the int pointer from where method will try to select the expression. </param>
    /// <returns>Span of the original array</returns>
    public static Span<TinyBasicToken> SelectExpressionFromLine(TinyBasicToken[] line, int start)
    {
        int pointerCopy = start;
        while (true)
        {
            if (pointerCopy >= line.Length)
            { return line.AsSpan(start, pointerCopy - start); }

            var token = line[pointerCopy];
            switch (token.Type)
            {
                case TokenType.ParenthesisClose:
                case TokenType.ParenthesisOpen:
                case TokenType.Number:
                {
                    ++pointerCopy;
                    break;
                }
                case TokenType.Operator:
                {
                    var op = token as OperatorToken;
                    if (op?.OperatorType is OperatorType.GreaterThan or OperatorType.GreaterThanOrEqual 
                        or OperatorType.LessThanOrEqual or OperatorType.LessThan or OperatorType.Equals or OperatorType.NotEqual)
                    { return line.AsSpan(start, pointerCopy - start); }
                    ++pointerCopy;
                    break;
                }
                case TokenType.String:
                {
                    var value = token.ToString();
                    if (value is "RND")
                    {
                        var funcSpan = FunctionParser.SelectFunctionTokens(line, pointerCopy);
                        pointerCopy += funcSpan.Length;
                    }
                    else if (char.TryParse(value, out _))
                    { ++pointerCopy; }
                    else
                    { return line.AsSpan(start, pointerCopy - start);  }

                    break;
                }
                default:
                { return line.AsSpan(start, pointerCopy - start); }
            }
        }
    }
    
    public static ExpressionToken ParseExpression(Span<TinyBasicToken> selectedTokens)
    {
        if (selectedTokens.Length is 0)
        { throw new EmptyExpressionException("Tried to parse an empty expression"); }
        
        List<TinyBasicToken> expression = [];
        int pointer = 0;
        try
        { ParseExpression(selectedTokens, ref pointer, expression); }
        catch (ParsingException ex)
        { throw new ParsingException($"Error parsing expression:\n >{ex.Message}"); }
        
        if (pointer + 1 < selectedTokens.Length)
        { throw new UnexpectedOrEmptyTokenException($"Unexpected token {selectedTokens[pointer + 1]} at the end of expression {ExpressionToString(expression)}"); }
        
        return new ExpressionToken(expression.ToArray());
    }

    private static void ParseExpression(Span<TinyBasicToken> selectedTokens, ref int pointer, List<TinyBasicToken> finalExpression)
    {
        ParseTerm(selectedTokens, ref pointer, finalExpression);
        
        while (pointer + 1 < selectedTokens.Length)
        {
            var op = selectedTokens[pointer + 1] as OperatorToken;
            if (op?.OperatorType is not (OperatorType.Plus or OperatorType.Minus))
            { return; }
            finalExpression.Add(op);
            
            ++pointer;
            if (pointer + 1 >= selectedTokens.Length)
            { throw new UnexpectedOrEmptyTokenException($"Expected a factor after operator: {ExpressionToString(finalExpression)}"); }
            
            ++pointer;
            ParseTerm(selectedTokens, ref pointer, finalExpression);
        }
    }

    private static void ParseTerm(Span<TinyBasicToken> selectedTokens, ref int pointer, List<TinyBasicToken> finalExpression)
    {
        ParseFactor(selectedTokens, ref pointer, finalExpression);
        
        while (pointer + 1 < selectedTokens.Length)
        {
            var op = selectedTokens[pointer + 1] as OperatorToken;
            if (op?.OperatorType is not (OperatorType.Division or OperatorType.Multiplication))
            { return; }
            finalExpression.Add(op);
            
            ++pointer;
            if (pointer + 1 >= selectedTokens.Length)
            { throw new UnexpectedOrEmptyTokenException($"Expected a factor after operator: {ExpressionToString(finalExpression)}"); }
            
            ++pointer;
            ParseFactor(selectedTokens, ref pointer, finalExpression);
        }
    }
    
    private static void ParseFactor(Span<TinyBasicToken> selectedTokens, ref int pointer, List<TinyBasicToken> finalExpression)
    {
        var token = selectedTokens[pointer];
        while (token is OperatorToken op && op.OperatorType is (OperatorType.Plus or OperatorType.Minus))
        {
            finalExpression.Add(token);
            ++pointer;
            if (pointer >= selectedTokens.Length)
            { throw new UnexpectedOrEmptyTokenException($"Unmatched unary operator: {ExpressionToString(finalExpression)}"); }

            token = selectedTokens[pointer];
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
                ++pointer;
                ParseExpression(selectedTokens, ref pointer, finalExpression);
                ++pointer;
                if (pointer >= selectedTokens.Length || selectedTokens[pointer].Type != TokenType.ParenthesisClose)
                { throw new UnexpectedOrEmptyTokenException($"Expected a closing parenthesis: {ExpressionToString(finalExpression)}"); }
                finalExpression.Add(selectedTokens[pointer]);

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
                { goto default; }
                
                var functionSpan = FunctionParser.SelectFunctionTokens(selectedTokens.ToArray(), pointer);
                try
                {
                    var functionToken = FunctionParser.ParseFunction(functionSpan);
                    finalExpression.Add(functionToken);
                }
                catch (ParsingException ex)
                { throw new ParsingException($"Error while parsing function in expression:\n >{ex.Message}"); }

                pointer += functionSpan.Length - 1;
                return;
            }
            default:
            { throw new UnexpectedOrEmptyTokenException($"Got unexpected token: {token}"); }
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