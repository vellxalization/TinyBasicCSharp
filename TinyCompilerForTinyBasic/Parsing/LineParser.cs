namespace TinyCompilerForTinyBasic.Parsing;

public class LineParser
{
    private TinyBasicToken[] _tokens;
    private int _pointer = 0;
    
    public LineParser(TinyBasicToken[] tokens) => _tokens = tokens;

    public TinyBasicToken[] ParseLine()
    {
        int pointerCopy = _pointer;
        TinyBasicToken token = _tokens[_pointer];
        TinyBasicToken? next = Peek();
        if (token.Type is TBTokenType.Number)
        {
            if ((next is null))
            { return _tokens[pointerCopy..(_pointer + 1)]; } // line with only number will be used ford deleting labels
            ++_pointer;
            if (next.Type is TBTokenType.NewLine)
            { return _tokens[pointerCopy.._pointer]; }
        }

        ParseStatement();
        next = Peek();
        if ((next is null))
        { return _tokens[pointerCopy..(_pointer + 1)]; }

        if (next.Type is TBTokenType.NewLine)
        {
            ++_pointer;
            return _tokens[pointerCopy..(_pointer++)];
        }
        
        throw new ParsingException("Expected a new line token");
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
    private void ParseStatement()
    {
        TinyBasicToken token = _tokens[_pointer];
        if (token.Type is not TBTokenType.String)
        { throw new ParsingException($"Expected a keyword: {LineToStringUtility.TokenToString(token)}"); }

        string value = ((ValueTinyBasicTinyBasicToken)token).Value;
        switch (value)
        {
            case "PRINT":
            {
                if (Peek() is null)
                { throw new ParsingException($"Expected a list of expressions after {value} keyword: {LineToStringUtility.TokenToString(token)}"); }

                ParseExprList();
                break;
            }
            case "IF":
                if (Peek() is null)
                { throw new ParsingException($"Expected an expression after IF keyword: {LineToStringUtility.TokenToString(token)}"); }
                
                ParseIf();
                break;
            case "GOSUB":
            case "GOTO":
            {
                if (Peek() is null)
                { throw new ParsingException($"Expected an expression after {value} keyword: {LineToStringUtility.TokenToString(token)}"); }
                
                ExpressionParser.ParseExpression(SelectExpression());
                return;
            }
            case "INPUT":
            {
                if (Peek() is null)
                { throw new ParsingException($"Expected a list of variables after INPUT keyword: {LineToStringUtility.TokenToString(token)}"); }

                ParseVarList();
                return;
            }
            case "LET":
            {
                if (Peek() is null)
                { throw new ParsingException($"Expected a variable name after LET keyword: {LineToStringUtility.TokenToString(token)}"); }

                ParseLet();
                return;
            }
            case "RETURN":
            case "CLEAR":
            case "LIST":
            case "RUN":
            case "END":
            { return; }
            default:
            { throw new ParsingException($"Unexpected keyword: {LineToStringUtility.TokenToString(token)}"); }
        }
    }

    private void ParseIf()
    {
        ExpressionParser.ParseExpression(SelectExpression());
        if (Peek()?.Type is not (TBTokenType.OperatorGreaterThan or TBTokenType.OperatorLessThan
            or TBTokenType.OperatorGreaterThanOrEqual or TBTokenType.OperatorLessThanOrEqual
            or TBTokenType.OperatorEquals or TBTokenType.OperatorNotEqual))
        { throw new ParsingException("Expected an operator after expression"); }
        
        ++_pointer;
        if (Peek() is null)
        { throw new ParsingException("Expected an expression after operator"); }
        ++_pointer;
        ExpressionParser.ParseExpression(SelectExpression());
        
        TinyBasicToken? next = Peek();
        if ((next is null) || (LineToStringUtility.TokenToString(next) is not "THEN"))
        { throw new ParsingException("Expected a THEN keyword after expression"); }
        ++_pointer;
        
        if (Peek() is null)
        { throw new ParsingException("Expected a statement after THEN keyword"); }
        ++_pointer;
        ParseStatement();
    }
    
    private void ParseExprList()
    {
        ExpressionParser.ParseExpression(SelectExpression());

        TinyBasicToken? next = Peek();
        while (next is not null)
        {
            if (next.Type is not TBTokenType.Comma)
            { return; }
            
            ++_pointer;
            if (Peek() is null)
            { throw new ParsingException($"Expected an expression after comma: {LineToStringUtility.TokenToString(next)}"); }
            ++_pointer;
            
            ExpressionParser.ParseExpression(SelectExpression());
            next = Peek();
        }
    }
    
    private void ParseVarList()
    {
        TinyBasicToken token = _tokens[_pointer];
        if (token.Type is not TBTokenType.String)
        { throw new ParsingException($"Expected a variable name: {LineToStringUtility.TokenToString(token)}"); }
        string value = ((ValueTinyBasicTinyBasicToken)token).Value;
        if ((!char.TryParse(value, out char address)) || (address is < 'A' or > 'Z'))
        { throw new ParsingException($"Expected a valid variable name: {LineToStringUtility.TokenToString(token)}"); }

        TinyBasicToken? next = Peek();
        while (next is not null)
        {
            if (next.Type is not TBTokenType.Comma)
            { return; }
            
            // continue only if after variable comes a comma
            ++_pointer;
            token = _tokens[_pointer];
            if (Peek() is null)
            { throw new ParsingException($"Expected a variable name: {LineToStringUtility.TokenToString(token)}"); }
            ++_pointer;
            token = _tokens[_pointer];
            if (token.Type is not TBTokenType.String)
            { throw new ParsingException($"Expected a variable name: {LineToStringUtility.TokenToString(token)}"); }
            value = ((ValueTinyBasicTinyBasicToken)token).Value;
            if ((!char.TryParse(value, out address)) || (address is < 'A' or > 'Z'))
            { throw new ParsingException($"Expected a valid variable name: {LineToStringUtility.TokenToString(token)}"); }

            next = Peek();
        }
    }
    
    private void ParseLet()
    {
        TinyBasicToken token = _tokens[_pointer];
        if (token.Type is not TBTokenType.String)
        { throw new ParsingException($"Expected a valid variable name: {LineToStringUtility.TokenToString(token)}"); }
        string value = ((ValueTinyBasicTinyBasicToken)token).Value;
        if (!char.TryParse(value, out char address) || (address is < 'A' or > 'Z'))
        { throw new ParsingException($"Expected a valid variable name: {LineToStringUtility.TokenToString(token)}"); }
        
        if (Peek() is null)
        { throw new ParsingException($"Expected an assignment operator after variable name: {LineToStringUtility.TokenToString(token)}"); }
        ++_pointer;
        token = _tokens[_pointer];
        if (token.Type is not TBTokenType.OperatorEquals)
        { throw new ParsingException($"Expected an assignment operator after variable name: {LineToStringUtility.TokenToString(token)}"); }
        
        if (Peek() is null)
        { throw new ParsingException($"Expected an expression after assignment operator: {LineToStringUtility.TokenToString(token)}"); }
        ++_pointer;
        ExpressionParser.ParseExpression(SelectExpression());
    }

    private Span<TinyBasicToken> SelectExpression()
    {
        int pointerCopy = _pointer;
        
        TinyBasicToken? next = Peek();
        while (next is not null)
        {
            switch (next.Type)
            {
                case TBTokenType.ParenthesisClose:
                case TBTokenType.ParenthesisOpen:
                case TBTokenType.OperatorPlus:
                case TBTokenType.OperatorMinus:
                case TBTokenType.OperatorDivision:
                case TBTokenType.OperatorMultiplication:
                case TBTokenType.Number:
                {
                    ++_pointer;
                    break;
                }
                case TBTokenType.String:
                {
                    string value = ((ValueTinyBasicTinyBasicToken)next).Value;
                    if ((char.TryParse(value, out char address)) && (address is >= 'A' and <= 'Z'))
                    { ++_pointer; }
                    else
                    { goto default; }

                    break;
                }
                default:
                { return new Span<TinyBasicToken>(_tokens, pointerCopy, (_pointer - pointerCopy + 1)); }
            }
            next = Peek();
        }
        return new Span<TinyBasicToken>(_tokens, pointerCopy, (_pointer - pointerCopy + 1));
    }
    
    private TinyBasicToken? Peek() => ((_pointer + 1) < _tokens.Length) ? _tokens[_pointer + 1] : null;
    
    public bool CanReadLine() => _pointer < _tokens.Length;
}