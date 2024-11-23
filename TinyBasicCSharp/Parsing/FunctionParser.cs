using System.Diagnostics;
using TinyCompilerForTinyBasic.Tokenization;

namespace TinyCompilerForTinyBasic.Parsing;

public class FunctionParser
{
    public static FunctionToken ParseFunction(Span<TinyBasicToken> selectedTokens)
    {
        if (selectedTokens.Length < 3)
        { throw new ParsingException("Function should contain at least 3 tokens (signature and a pair of parentheses)"); }
        
        if (selectedTokens[0].Type is not TokenType.String)
        { throw new UnexpectedOrEmptyTokenException("Expected a string function name"); }
        var signature = selectedTokens[0].ToString();
        
        if (selectedTokens[1].Type is not TokenType.ParenthesisOpen)
        { throw new UnexpectedOrEmptyTokenException($"Expected an open parenthesis after function name {signature}"); }
        if (selectedTokens[^1].Type is not TokenType.ParenthesisClose)
        { throw new UnexpectedOrEmptyTokenException($"Expected a closing parenthesis after arguments for function {signature}"); }
        
        if (signature is not "RND")
        { throw new ParsingException($"Unknown function name {signature}"); }
        
        TinyBasicToken[][] arguments = [];
        if (selectedTokens.Length > 3)
        {
            var argsSlice = selectedTokens.Slice(2, selectedTokens.Length - 3);
            try
            { arguments = SliceArguments(argsSlice); }
            catch (ParsingException ex)
            { throw new ParsingException($"Error parsing arguments for {signature} function:\n >{ex.Message}"); }
        }

        FunctionToken token;
        switch (signature)
        {
            case "RND":
            {
                try
                { token = ParseArgsForRandom(arguments); }
                catch (ParsingException ex)
                { throw new ParsingException($"Error while parsing arguments for RND function:\n >{ex.Message}"); }

                break;
            }
            default:
            { throw new UnexpectedOrEmptyTokenException($"Unknown function name {signature}"); }
        }

        return token;
    }

    private static FunctionToken ParseArgsForRandom(TinyBasicToken[][] arguments)
    {
        if (arguments.Length != 1)
        { throw new ParsingException($"Expected 1 argument for RND function, got {arguments.Length}"); }

        try
        {
            var expression = ExpressionParser.ParseExpression(arguments[0]);
            return new FunctionToken([expression], "RND");
        }
        catch (ParsingException ex)
        { throw new ParsingException($"Error parsing expression for RND argument:\n >{ex.Message}"); }
    }
    
    private static TinyBasicToken[][] SliceArguments(Span<TinyBasicToken> argsSlice)
    {
        List<TinyBasicToken[]> arguments = [];
        int pointer = 0;
        int i = 0;
        for (; i < argsSlice.Length; ++i)
        {
            var token = argsSlice[i];
            if (token.Type is not TokenType.Comma)
            { continue; }

            if (i + 1 >= argsSlice.Length ||
                argsSlice[i + 1].Type is TokenType.NewLine or TokenType.Comma or TokenType.ParenthesisClose)
            { throw new UnexpectedOrEmptyTokenException("Expected next argument after comma"); }
            
            arguments.Add(argsSlice.Slice(pointer, i - pointer).ToArray());
            pointer = i + 1;
        }
        
        arguments.Add(argsSlice.Slice(pointer, i - pointer).ToArray());
        return arguments.ToArray();
    }
    
    public static Span<TinyBasicToken> SelectFunctionTokens(TinyBasicToken[] tokens, int startFrom)
    {
        if (tokens[startFrom].Type is not TokenType.String)
        { return []; }

        int pointerCopy = startFrom;
        ++startFrom;
        if (startFrom >= tokens.Length || tokens[startFrom].Type is not TokenType.ParenthesisOpen)
        { return tokens.AsSpan(pointerCopy, startFrom - pointerCopy); }
        ++startFrom;

        int parenthesisCount = 1;
        while (true)
        {
            if (startFrom >= tokens.Length)
            { return tokens.AsSpan(pointerCopy, startFrom - pointerCopy); }
            
            var token = tokens[startFrom];
            switch (token.Type)
            {
                case TokenType.NewLine:
                { return tokens.AsSpan(pointerCopy, startFrom - pointerCopy + 1); }
                case TokenType.ParenthesisOpen:
                {
                    ++parenthesisCount;
                    break;
                }
                case TokenType.ParenthesisClose:
                {
                    --parenthesisCount;
                    if (parenthesisCount == 0)
                    { return tokens.AsSpan(pointerCopy, startFrom - pointerCopy + 1); }

                    break;
                }
            }
            ++startFrom;
        }
    }
}