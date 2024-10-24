using System.Text;
using TinyCompilerForTinyBasic.Tokenization;

namespace TinyCompilerForTinyBasic.Parsing;

public class LineParser
{
    private TinyBasicToken[] _tokens;
    private int _pointer = 0;
    
    public LineParser(TinyBasicToken[] tokens) => _tokens = tokens;

    public bool ParseLine(out TinyBasicToken[] result, out string? error)
    {
        error = null;
        
        TinyBasicToken token = _tokens[_pointer];
        if (token.Type is TBTokenType.NewLine)
        {
            ++_pointer;
            result = [token];
            return true;
        }
        
        List<TinyBasicToken?> line = new();
        TinyBasicToken? next;
        if (token.Type is TBTokenType.Number)
        {
            line.Add(token);
            int value = int.Parse(token.ToString());
            if (value is < 0 or > short.MaxValue)
            {
                error = $"Invalid label number: {value}";
                result = line.ToArray()!;
                return false;
            }

            next = Peek();
            if (next is null)
            {
                result = line.ToArray()!;
                return true;
            } 
            
            if (next.Type is TBTokenType.NewLine)
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
        if (next is null || next.Type is TBTokenType.NewLine)
        {
            _pointer += 2;
            result = line.ToArray()!;
            return true;
        }
        
        error = $"Expected a new line at \"{LineToString(line)}\"";
        result = line.ToArray()!;
        return false;
    }
    
    // statement ::= PRINT expr-list
    //     IF expression relop expression THEN statement
    //     GOTO expression
    //     INPUT var-list
    //     LET var = expression
    //     GOSUB expression
    //     RETURN
    //     CLEAR
    //     LIST
    //     RUN
    //     END
    private void ParseStatement(List<TinyBasicToken?> lineToModify)
    {
        TinyBasicToken token = _tokens[_pointer];
        lineToModify.Add(token);
        
        string value = token.ToString();
        switch (value)
        {
            case "PRINT":
            {
                if (Peek() is null)
                { throw new ParsingException($"Expected a list of expressions after PRINT keyword at \"{LineToString(lineToModify)}\""); }
                
                ++_pointer;
                ParseExprList(lineToModify);
                break;
            }
            case "IF":
                if (Peek() is null)
                { throw new ParsingException($"Expected an expression after IF keyword at \"{LineToString(lineToModify)}\""); }
                
                ++_pointer;
                ParseIf(lineToModify);
                break;
            case "GOSUB":
            case "GOTO":
            {
                if (Peek() is null)
                { throw new ParsingException($"Expected an expression after {value} keyword at \"{LineToString(lineToModify)}\""); }

                ++_pointer;
                ExpressionTinyBasicToken expression = ParsingUtils.SelectExpressionFromLine(_tokens, ref _pointer);
                try
                { ParsingUtils.ParseExpression(expression); }
                catch (ParsingException ex)
                { throw new ParsingException($"Failed to parse expression at \"{LineToString(lineToModify)}\": {ex.Message}"); }
                lineToModify.Add(expression);
                return;
            }
            case "INPUT":
            {
                if (Peek() is null)
                { throw new ParsingException($"Expected a list of variables after INPUT keyword at \"{LineToString(lineToModify)}\""); }

                ++_pointer;
                ParseVarList(lineToModify);
                return;
            }
            case "LET":
            {
                if (Peek() is null)
                { throw new ParsingException($"Expected a variable name after LET keyword at \"{LineToString(lineToModify)}\""); }

                ++_pointer;
                ParseLet(lineToModify);
                return;
            }
            case "RETURN":
            case "CLEAR":
            case "LIST":
            case "RUN":
            case "END":
            { return; }
            default:
            { throw new ParsingException($"Unexpected keyword \"{token}\" at \"{LineToString(lineToModify)}\""); }
        }
    }

    private void ParseIf(List<TinyBasicToken?> lineToModify)
    {
        ExpressionTinyBasicToken expression = ParsingUtils.SelectExpressionFromLine(_tokens, ref _pointer);
        try
        { ParsingUtils.ParseExpression(expression); }
        catch (ParsingException ex)
        { throw new ParsingException($"Failed to parse expression at \"{LineToString(lineToModify)}\": {ex.Message}"); }
        lineToModify.Add(expression);

        TinyBasicToken? next = Peek();
        lineToModify.Add(next);
        if (next?.Type is not (TBTokenType.OperatorGreaterThan or TBTokenType.OperatorLessThan
            or TBTokenType.OperatorGreaterThanOrEqual or TBTokenType.OperatorLessThanOrEqual
            or TBTokenType.OperatorEquals or TBTokenType.OperatorNotEqual))
        { throw new ParsingException($"Expected an operator after expression at \"{LineToString(lineToModify)}\""); }
        
        ++_pointer;
        next = Peek();
        if (next is null)
        { throw new ParsingException($"Expected an expression after operator at \"{LineToString(lineToModify)}\""); }
        ++_pointer;
        
        expression = ParsingUtils.SelectExpressionFromLine(_tokens, ref _pointer);
        try
        { ParsingUtils.ParseExpression(expression); }
        catch (ParsingException ex)
        { throw new ParsingException($"Failed to parse expression at \"{LineToString(lineToModify)}\": {ex.Message}"); }
        lineToModify.Add(expression);

        next = Peek();
        lineToModify.Add(next);
        if (next?.ToString() is not "THEN")
        { throw new ParsingException($"Expected a THEN keyword after expression at \"{LineToString(lineToModify)}\""); }
        ++_pointer;
        
        if (next is null)
        { throw new ParsingException($"Expected a statement after THEN keyword at \"{LineToString(lineToModify)}\""); }
        ++_pointer;
        ParseStatement(lineToModify);
    }
    
    private void ParseExprList(List<TinyBasicToken?> lineToModify)
    {
        TinyBasicToken token = _tokens[_pointer];
        if (token.Type is TBTokenType.QuotedString)
        { lineToModify.Add(token); }
        else
        {
            ExpressionTinyBasicToken expression = ParsingUtils.SelectExpressionFromLine(_tokens, ref _pointer);
            try
            { ParsingUtils.ParseExpression(expression); }
            catch (ParsingException ex)
            { throw new ParsingException($"Failed to parse expression at \"{LineToString(lineToModify)}\": {ex.Message}"); }
            lineToModify.Add(expression);
        }
        
        TinyBasicToken? next = Peek();
        while (next?.Type is TBTokenType.Comma)
        {
            lineToModify.Add(next);
            
            ++_pointer;
            if (Peek() is null)
            { throw new ParsingException($"Expected an expression after comma at \"{LineToString(lineToModify)}\""); }
            
            ++_pointer;
            token = _tokens[_pointer];
            if (token.Type is TBTokenType.QuotedString)
            { lineToModify.Add(token); }
            else
            {
                ExpressionTinyBasicToken expression = ParsingUtils.SelectExpressionFromLine(_tokens, ref _pointer);
                try
                { ParsingUtils.ParseExpression(expression); }
                catch (ParsingException ex)
                { throw new ParsingException($"Failed to parse expression at \"{LineToString(lineToModify)}\": {ex.Message}"); }
                lineToModify.Add(expression);
            }
            next = Peek();
        }
    }
    
    private void ParseVarList(List<TinyBasicToken?> lineToModify)
    {
        TinyBasicToken token = _tokens[_pointer];
        lineToModify.Add(token);
        if ((!char.TryParse(token.ToString(), out char address)) || (address is < 'A' or > 'Z'))
        { throw new ParsingException($"Expected a valid variable name at \"{LineToString(lineToModify)}\""); }

        TinyBasicToken? next = Peek();
        while (next?.Type is TBTokenType.Comma)
        {
            lineToModify.Add(next);
            
            ++_pointer;
            next = Peek();
            if ((!char.TryParse(next?.ToString(), out address)) || (address is < 'A' or > 'Z'))
            { throw new ParsingException($"Expected a valid variable name at \"{LineToString(lineToModify)}\""); }
            lineToModify.Add(next);
            
            ++_pointer;
            next = Peek();
        }
    }
    
    private void ParseLet(List<TinyBasicToken?> lineToModify)
    {
        TinyBasicToken token = _tokens[_pointer];
        lineToModify.Add(token);
        if (!char.TryParse(token.ToString(), out char address) || (address is < 'A' or > 'Z'))
        { throw new ParsingException($"Expected a valid variable name at \"{LineToString(lineToModify)}\""); }
        
        TinyBasicToken? next = Peek();
        lineToModify.Add(next);
        if (next?.Type is not TBTokenType.OperatorEquals)
        { throw new ParsingException($"Expected an assignment operator after variable name at \"{LineToString(lineToModify)}\""); }
        ++_pointer;
        
        next = Peek();
        if (next is null)
        { throw new ParsingException($"Expected an expression after assignment operator at \"{LineToString(lineToModify)}\""); }
        ++_pointer;
        
        ExpressionTinyBasicToken expression = ParsingUtils.SelectExpressionFromLine(_tokens, ref _pointer);
        try
        { ParsingUtils.ParseExpression(expression); }
        catch (ParsingException ex)
        { throw new ParsingException($"Failed to parse expression at \"{LineToString(lineToModify)}\": {ex.Message}"); }
        lineToModify.Add(expression);
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