namespace TinyBasicCSharp.Tokenization;

public static class Lexer
{
    public static IToken[] Tokenize(string source)
    {
        var tokens = new List<IToken>();
        int i = 0;
        while (i < source.Length)
        {
            var character = source[i];
            if (char.IsDigit(character))
            { tokens.Add(GetNumberToken(source, ref i)); }
            else if (character == '"')
            { tokens.Add(GetQuotedStringToken(source, ref i)); }
            else if (character is ('+' or '-' or '*' or '/' or '=' or '<' or '>'))
            { tokens.Add(GetOperatorToken(source, ref i)); }
            else if (character is ' ' or '\t')
            { ++i; }
            else if (char.IsWhiteSpace(character) || character is ',' or '(' or ')')
            { tokens.Add(GetServiceToken(source, ref i)); }
            else
            { tokens.Add(GetWordToken(source, ref i)); }
        }
        return tokens.ToArray();
    }

    private static WordToken GetWordToken(string source, ref int startFrom)
    {
        int start = startFrom;
        var character = source[startFrom];
        while (!char.IsDigit(character) 
               && !char.IsWhiteSpace(character) 
               && character is not ('(' or ')' or ',' or '+' or '-' or '*' or '/' or '=' or '<' or '>'))
        {
            ++startFrom;
            if (startFrom >= source.Length) 
            { break; }
            character = source[startFrom];
        }
        
        return new WordToken(source[start..startFrom]);
    }

    private static NumberToken GetNumberToken(string source, ref int startFrom)
    {
        int start = startFrom;
        var character = source[startFrom];
        while (char.IsDigit(character))
        {
            ++startFrom;
            if (startFrom >= source.Length)
            { break; }
            character = source[startFrom];
        }
        
        var value = int.Parse(source[start..startFrom]);
        return new NumberToken(value);
    }

    private static QuotedStringToken GetQuotedStringToken(string source, ref int startFrom)
    {
        if (source[startFrom] != '"')
        { throw new ArgumentException("Expected to start from quotation mark"); }
        
        int start = startFrom;
        ++startFrom;
        while (startFrom < source.Length && source[startFrom] is not ('"' or '\r' or '\n'))
        { ++startFrom; }
        
        if (startFrom < source.Length)
        { ++startFrom; }
        
        var value = source[start..startFrom];
        if (value.Length > 1 && value[^1] == '"')
        { return new QuotedStringToken(value); }
        
        throw new TokenizationException($"Failed to find matching quotation mark for the string: {value}");
    }

    private static OperatorToken GetOperatorToken(string source, ref int startFrom)
    {
        var op = source[startFrom];
        ++startFrom;
        switch (op)
        {
            case '+':
            { return new OperatorToken(OperatorType.Plus); }
            case '-':
            { return new OperatorToken(OperatorType.Minus); }
            case '*':
            { return new OperatorToken(OperatorType.Multiplication); }
            case '/':
            { return new OperatorToken(OperatorType.Division); }
            case '=':
            { return new OperatorToken(OperatorType.Equals); }
            case '<':
            {
                if (startFrom >= source.Length)
                { return new OperatorToken(OperatorType.LessThan); }
                if (source[startFrom] == '=')
                {
                    ++startFrom;
                    return new OperatorToken(OperatorType.LessThanOrEqual);
                }
                if (source[startFrom] == '>')
                {
                    ++startFrom;
                    return new OperatorToken(OperatorType.NotEqual);
                }
                return new OperatorToken(OperatorType.LessThan);
            }
            case '>':
            {
                if (startFrom >= source.Length)
                { return new OperatorToken(OperatorType.GreaterThan); }
                if (source[startFrom] == '=')
                {
                    ++startFrom;
                    return new OperatorToken(OperatorType.GreaterThanOrEqual);
                }
                if (source[startFrom] == '<')
                {
                    ++startFrom;
                    return new OperatorToken(OperatorType.NotEqual);
                }
                return new OperatorToken(OperatorType.GreaterThan);
            }
            default:
            { throw new TokenizationException($"Unknown operator: {op}"); }
        }
    }

    private static ServiceToken GetServiceToken(string source, ref int startFrom)
    {
        var character = source[startFrom];
        ++startFrom;
        switch (character)
        {
            case '(':
            { return new ServiceToken(ServiceType.ParenthesisOpen); }
            case ')':
            { return new ServiceToken(ServiceType.ParenthesisClose); }
            case ',':
            { return new ServiceToken(ServiceType.Comma); }
            case '\r':
            {
                if (source[startFrom] == '\n')
                { ++startFrom; }
                return new ServiceToken(ServiceType.Newline);
            }
            case '\n':
            {
                if (source[startFrom] == '\r')
                { ++startFrom; }
                return new ServiceToken(ServiceType.Newline);
            }
            default:
            { throw new TokenizationException($"Unexpected service token: {character}"); }
        }
    }
}