using System.Text;
using TinyCompilerForTinyBasic.Tokenization;

namespace TinyCompilerForTinyBasic.Parsing;

/// <summary>
/// Class for parsing array of tokens
/// </summary>
public class LineParser
{
    private TinyBasicToken[] _tokens;
    private int _pointer = 0;
    
    public LineParser(TinyBasicToken[] tokens) => _tokens = tokens;

    /// <summary>
    /// Parses a single line of code, separated by new line
    /// </summary>
    /// <param name="result">An array of tokens containing all successfully parsed tokens</param>
    /// <param name="error">Error message if parsing was failed</param>
    /// <returns>Was parsing successful</returns>
    public bool ParseLine(out TinyBasicToken[] result, out string? error)
    {
        error = null;
        
        TinyBasicToken token = _tokens[_pointer];
        if (token.Type is TokenType.NewLine)
        {
            ++_pointer;
            result = [token];
            return true;
        }
        
        List<TinyBasicToken?> line = new();
        TinyBasicToken? next;
        if (token.Type is TokenType.Number)
        {
            try
            { ParseLabel(line); }
            catch (ParsingException ex)
            {
                result = line.ToArray()!;
                error = ex.Message;
                return false;
            }

            next = Peek();
            if (next is null || next.Type is TokenType.NewLine)
            {
                _pointer += 2;
                result = line.ToArray()!;
                return true;
            } 
            
            ++_pointer;
        }

        try
        { ParseStatement(line); }
        catch (ParsingException ex)
        {
            error = ex.Message;
            result = line.ToArray()!;
            return false;
        }
        
        next = Peek();
        if (next is null || next.Type is TokenType.NewLine)
        {
            _pointer += 2;
            result = line.ToArray()!;
            return true;
        }
        
        error = $"Expected a new line after \"{LineToString(line)}\"";
        result = line.ToArray()!;
        return false;
    }

    private void ParseLabel(List<TinyBasicToken?> lineToModify)
    {
        var token = _tokens[_pointer];
        lineToModify.Add(token);
        
        if (token?.Type is not TokenType.Number)
        { throw new UnexpectedOrEmptyTokenException($"Label {token} should be a number"); }
        
        var value = int.Parse(token.ToString());
        if (value is < 1 or > short.MaxValue)
        { throw new InvalidLabelException("Label should be more than 0 and less than 32768"); }
    }
    
    private void ParseStatement(List<TinyBasicToken?> lineToModify)
    {
        TinyBasicToken token = _tokens[_pointer];
        
        string value = token.ToString();
        switch (value)
        {
            case "REM":
            {
                lineToModify.Add(token);
                ++_pointer;
                ParseRem(lineToModify);
                return;
            }
            case "PRINT":
            {
                lineToModify.Add(token);
                if (Peek() is null)
                { throw new UnexpectedOrEmptyTokenException($"Expected a list of expressions after PRINT keyword at \"{LineToString(lineToModify)}\""); }
                
                ++_pointer;
                ParseExprList(lineToModify);
                return;
            }
            case "IF":
                lineToModify.Add(token);
                if (Peek() is null)
                { throw new UnexpectedOrEmptyTokenException($"Expected an expression after IF keyword at \"{LineToString(lineToModify)}\""); }
                
                ++_pointer;
                ParseIf(lineToModify);
                return;
            case "GOSUB":
            case "GOTO":
            {
                lineToModify.Add(token);
                if (Peek() is null)
                { throw new UnexpectedOrEmptyTokenException($"Expected an expression after {value} keyword at \"{LineToString(lineToModify)}\""); }

                ++_pointer;
                Span<TinyBasicToken> expressionSpan = ExpressionParser.SelectExpressionFromLine(_tokens, _pointer);
                try
                {
                    var expression = ExpressionParser.ParseExpression(expressionSpan);
                    _pointer += expressionSpan.Length;
                    lineToModify.Add(expression);
                }
                catch (ParsingException ex)
                { throw new ParsingException($"Failed to parse expression at \"{LineToString(lineToModify)}\":\n >{ex.Message}"); }
                return;
            }
            case "INPUT":
            {
                lineToModify.Add(token);
                if (Peek() is null)
                { throw new UnexpectedOrEmptyTokenException($"Expected a list of variables after INPUT keyword at \"{LineToString(lineToModify)}\""); }

                ++_pointer;
                ParseVarList(lineToModify);
                return;
            }
            case "LET":
            {
                lineToModify.Add(token);
                if (Peek() is null)
                { throw new UnexpectedOrEmptyTokenException($"Expected a variable name after LET keyword at \"{LineToString(lineToModify)}\""); }

                ++_pointer;
                ParseLet(lineToModify);
                return;
            }
            case "RETURN":
            case "CLEAR":
            case "LIST":
            case "RUN":
            case "END":
            {
                lineToModify.Add(token);
                return;
            }
            default:
            { throw new UnexpectedOrEmptyTokenException($"Unexpected keyword \"{token}\" at \"{LineToString(lineToModify)}\""); }
        }
    }

    private void ParseRem(List<TinyBasicToken?> lineToModify)
    {
        StringBuilder builder = new();
        TinyBasicToken? next = Peek();
        while (next is not null && next.Type is not TokenType.NewLine)
        {
            builder.Append(next);
            builder.Append(' ');
            ++_pointer;
            next = Peek();
        }
        lineToModify.Add(new ValueToken(TokenType.QuotedString, builder.ToString()));
    }
    
    private void ParseIf(List<TinyBasicToken?> lineToModify)
    {
        var expressionSpan = ExpressionParser.SelectExpressionFromLine(_tokens, _pointer);
        ExpressionToken expression;
        try
        { expression = ExpressionParser.ParseExpression(expressionSpan); }
        catch (ParsingException ex)
        { throw new ParsingException($"Failed to parse expression at \"{LineToString(lineToModify)}\":\n >{ex.Message}"); }
        _pointer += expressionSpan.Length;
        lineToModify.Add(expression);

        TinyBasicToken? next = Peek();
        if (next?.Type is not (TokenType.OperatorGreaterThan or TokenType.OperatorLessThan
            or TokenType.OperatorGreaterThanOrEqual or TokenType.OperatorLessThanOrEqual
            or TokenType.OperatorEquals or TokenType.OperatorNotEqual))
        { throw new UnexpectedOrEmptyTokenException($"Expected an operator after expression at \"{LineToString(lineToModify)}\""); }
        lineToModify.Add(next);
        
        ++_pointer;
        next = Peek();
        if (next is null)
        { throw new UnexpectedOrEmptyTokenException($"Expected an expression after operator at \"{LineToString(lineToModify)}\""); }
        
        ++_pointer;
        expressionSpan = ExpressionParser.SelectExpressionFromLine(_tokens, _pointer);
        try
        { expression = ExpressionParser.ParseExpression(expressionSpan); }
        catch (ParsingException ex)
        { throw new ParsingException($"Failed to parse expression at \"{LineToString(lineToModify)}\":\n >{ex.Message}"); }
        _pointer += expressionSpan.Length;
        lineToModify.Add(expression);

        next = Peek();
        if (next?.ToString() is not "THEN")
        { throw new UnexpectedOrEmptyTokenException($"Expected a THEN keyword after expression at \"{LineToString(lineToModify)}\""); }
        lineToModify.Add(next); 
        
        ++_pointer;
        next = Peek();
        if (next is null)
        { throw new UnexpectedOrEmptyTokenException($"Expected a statement after THEN keyword at \"{LineToString(lineToModify)}\""); }
        
        ++_pointer;
        ParseStatement(lineToModify);
    }
    
    private void ParseExprList(List<TinyBasicToken?> lineToModify)
    {
        TinyBasicToken token = _tokens[_pointer];
        if (token.Type is TokenType.QuotedString)
        { lineToModify.Add(token); }
        else
        {
            var expressionSpan = ExpressionParser.SelectExpressionFromLine(_tokens, _pointer);
            try
            {
                var expression = ExpressionParser.ParseExpression(expressionSpan);
                lineToModify.Add(expression);
            }
            catch (ParsingException ex)
            { throw new ParsingException($"Failed to parse expression at \"{LineToString(lineToModify)}\":\n >{ex.Message}"); }

            _pointer += expressionSpan.Length - 1;
        }
        
        TinyBasicToken? next = Peek();
        while (next?.Type is TokenType.Comma)
        {
            lineToModify.Add(next);
            
            ++_pointer;
            if (Peek() is null)
            { throw new UnexpectedOrEmptyTokenException($"Expected an expression or quoted string after comma at \"{LineToString(lineToModify)}\""); }
            
            ++_pointer;
            token = _tokens[_pointer];
            if (token.Type is TokenType.QuotedString)
            { lineToModify.Add(token); }
            else
            {
                var expressionSpan = ExpressionParser.SelectExpressionFromLine(_tokens, _pointer);
                try
                {
                    var expression = ExpressionParser.ParseExpression(expressionSpan);
                    lineToModify.Add(expression);
                }
                catch (ParsingException ex)
                { throw new ParsingException($"Failed to parse expression at \"{LineToString(lineToModify)}\":\n >{ex.Message}"); }

                _pointer += expressionSpan.Length;
            }
            next = Peek();
        }
    }
    
    private void ParseVarList(List<TinyBasicToken?> lineToModify)
    {
        TinyBasicToken token = _tokens[_pointer];
        if ((!char.TryParse(token.ToString(), out char address)) || (address is < 'A' or > 'Z'))
        { throw new InvalidVariableNameException($"Expected a valid variable name at \"{LineToString(lineToModify)}\""); }
        lineToModify.Add(token);

        TinyBasicToken? next = Peek();
        while (next?.Type is TokenType.Comma)
        {
            lineToModify.Add(next);
            
            ++_pointer;
            next = Peek();
            lineToModify.Add(next);
            if ((!char.TryParse(next?.ToString(), out address)) || (address is < 'A' or > 'Z'))
            { throw new InvalidVariableNameException($"Expected a valid variable name at \"{LineToString(lineToModify)}\""); }
            
            ++_pointer;
            next = Peek();
        }
    }
    
    private void ParseLet(List<TinyBasicToken?> lineToModify)
    {
        TinyBasicToken token = _tokens[_pointer];
        if (!char.TryParse(token.ToString(), out char address) || (address is < 'A' or > 'Z'))
        { throw new InvalidVariableNameException($"Expected a valid variable name at \"{LineToString(lineToModify)}\""); }
        lineToModify.Add(token);
        
        TinyBasicToken? next = Peek();
        if (next?.Type is not TokenType.OperatorEquals)
        { throw new UnexpectedOrEmptyTokenException($"Expected an assignment operator after variable name at \"{LineToString(lineToModify)}\""); }
        lineToModify.Add(next);
        
        ++_pointer;
        next = Peek();
        if (next is null)
        { throw new UnexpectedOrEmptyTokenException($"Expected an expression after assignment operator at \"{LineToString(lineToModify)}\""); }
        ++_pointer;
        
        var expressionSpan = ExpressionParser.SelectExpressionFromLine(_tokens, _pointer);
        try
        {
            var expression = ExpressionParser.ParseExpression(expressionSpan);
            lineToModify.Add(expression);
        }
        catch (ParsingException ex)
        { throw new ParsingException($"Failed to parse expression at \"{LineToString(lineToModify)}\":\n >{ex.Message}"); }

        _pointer += expressionSpan.Length;
    }
    
    private string LineToString(IEnumerable<TinyBasicToken?> line)
    {
        var builder = new StringBuilder();
        foreach (TinyBasicToken? token in line)
        {
            builder.Append(token is null ? "" : token.ToString());
            builder.Append(' ');
        }
        
        builder.Remove(builder.Length - 1, 1);
        return builder.ToString();
    }
    
    private TinyBasicToken? Peek() => ((_pointer + 1) < _tokens.Length) ? _tokens[_pointer + 1] : null;
    
    public bool CanReadLine() => _pointer < _tokens.Length;
}