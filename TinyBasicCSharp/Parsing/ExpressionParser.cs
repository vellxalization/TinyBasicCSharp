using System.Text;
using TinyBasicCSharp.Tokenization;

namespace TinyBasicCSharp.Parsing;

public class ExpressionParser
{
    /// <summary>
    /// Matches all possible expression tokens (operators, variables, numbers, function calls) and returns span of them.
    /// </summary>
    /// <param name="line">Array of tokens</param>
    /// <param name="start">int pointer from where method will try to select the expression. </param>
    /// <returns>Span of the original array</returns>
    public static Span<IToken> SelectExpressionFromLine(Span<IToken> line, int start)
    {
        int pointerCopy = start;
        while (pointerCopy < line.Length)
        {
            var token = line[pointerCopy];
            switch (token)
            {
                case WordToken word:
                {
                    if (char.TryParse(word.Value, out var address) && address is >= 'A' and <= 'Z')
                    {
                        ++pointerCopy;
                        continue;
                    }
                    if (!FunctionParser.IsValidFunctionName(word.Value))
                    { return line.Slice(start, pointerCopy - start); }
                    
                    var funcSpan = FunctionParser.SelectFunctionTokens(line, pointerCopy);
                    pointerCopy += funcSpan.Length;
                    break;
                }
                case NumberToken 
                    or ServiceToken { Type: ServiceType.ParenthesisOpen or ServiceType.ParenthesisClose } 
                    or OperatorToken { Type: OperatorType.Plus or OperatorType.Minus or OperatorType.Division or OperatorType.Multiplication }:
                {
                    ++pointerCopy;
                    break;
                }
                default:
                { return line.Slice(start, pointerCopy - start); }
            }
        }
        return line.Slice(start, pointerCopy - start);
    }
    
    public static ExpressionToken ParseExpression(Span<IToken> selectedTokens)
    {
        if (selectedTokens.Length is 0)
        { throw new ArgumentException("Tried to parse an empty expression"); }
        
        var expression = new List<IToken>(selectedTokens.Length);
        int pointer = 0;
        try
        { ParseExpression(selectedTokens, ref pointer, expression); }
        catch (ParsingException ex)
        { throw new ParsingException("Error parsing expression", ex); }
        
        if (pointer < selectedTokens.Length)
        { throw new UnexpectedTokenException($"Unexpected token {selectedTokens[pointer]} at the end of expression {ExpressionToString(expression)}"); }
        
        return new ExpressionToken(expression.ToArray());
    }

    private static void ParseExpression(Span<IToken> selectedTokens, ref int pointer, List<IToken> finalExpression)
    {
        ParseTerm(selectedTokens, ref pointer, finalExpression);
        
        while (pointer < selectedTokens.Length 
               && selectedTokens[pointer] is OperatorToken { Type: OperatorType.Plus or OperatorType.Minus } op)
        {
            finalExpression.Add(op);
            ++pointer;
            if (pointer >= selectedTokens.Length)
            { throw new UnexpectedTokenException($"Expected a factor after operator: {ExpressionToString(finalExpression)}"); }
            
            ParseTerm(selectedTokens, ref pointer, finalExpression);
        }
    }

    private static void ParseTerm(Span<IToken> selectedTokens, ref int pointer, List<IToken> finalExpression)
    {
        ParseFactor(selectedTokens, ref pointer, finalExpression);
        
        while (pointer < selectedTokens.Length 
               && selectedTokens[pointer] is OperatorToken { Type: OperatorType.Division or OperatorType.Multiplication } op)
        {
            finalExpression.Add(op);
            ++pointer;
            if (pointer >= selectedTokens.Length)
            { throw new UnexpectedTokenException($"Expected a factor after operator: {ExpressionToString(finalExpression)}"); }
            
            ParseFactor(selectedTokens, ref pointer, finalExpression);
        }
    }
    
    private static void ParseFactor(Span<IToken> selectedTokens, ref int pointer, List<IToken> finalExpression)
    {
        var token = selectedTokens[pointer];
        while (token is OperatorToken { Type: OperatorType.Plus or OperatorType.Minus })
        {
            finalExpression.Add(token);
            ++pointer;
            if (pointer >= selectedTokens.Length)
            { throw new UnexpectedTokenException($"Unmatched unary operator: {ExpressionToString(finalExpression)}"); }

            token = selectedTokens[pointer];
        }

        switch (token)
        {
            case NumberToken:
            {
                finalExpression.Add(token);
                ++pointer;
                return;
            }
            case ServiceToken { Type: ServiceType.ParenthesisOpen }:
            {
                finalExpression.Add(token);
                ++pointer;
                ParseExpression(selectedTokens, ref pointer, finalExpression);
                if (pointer >= selectedTokens.Length || selectedTokens[pointer] is not ServiceToken { Type: ServiceType.ParenthesisClose })
                { throw new UnexpectedTokenException($"Expected a closing parenthesis: {ExpressionToString(finalExpression)}"); }
                
                finalExpression.Add(selectedTokens[pointer]);
                ++pointer;
                return;
            }
            case WordToken word:
            {
                if (char.TryParse(word.Value, out char address) && address is >= 'A' and <= 'Z')
                {
                    finalExpression.Add(token);
                    ++pointer;
                    return;
                }
                if (!FunctionParser.IsValidFunctionName(word.Value))
                { goto default; }
                
                var functionSpan = FunctionParser.SelectFunctionTokens(selectedTokens.ToArray(), pointer);
                var functionToken = FunctionParser.ParseFunction(functionSpan);
                finalExpression.Add(functionToken);
                pointer += functionSpan.Length;
                return;
            }
            default:
            { throw new UnexpectedTokenException($"Got unexpected token: {token}"); }
        }
    }

    private static string ExpressionToString(IEnumerable<IToken> expression)
    {
        var sb = new StringBuilder();
        foreach (var token in expression)
        { sb.Append(token); }
        
        return sb.ToString();
    }
}