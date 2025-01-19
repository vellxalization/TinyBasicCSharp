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
    public bool ParseLine(out Statement result, out string? error)
    {
        error = null;
        
        TinyBasicToken token = _tokens[_pointer];
        if (token.Type is TokenType.NewLine)
        {
            ++_pointer;
            result = new Statement(null, StatementType.Newline, []);
            return true;
        }
        
        TinyBasicToken? next;
        short? label = null;
        if (token.Type is TokenType.Number)
        {
            try
            { label = ParseLabel(); }
            catch (ParsingException ex)
            {
                result = new Statement();
                error = ex.Message;
                return false;
            }

            next = Peek();
            if (next is null || next.Type is TokenType.NewLine)
            {
                _pointer += 2;
                result = new Statement(label, StatementType.Newline, []);
                return true;
            } 
            
            ++_pointer;
        }

        StatementType type;
        TinyBasicToken[] arguments;
        try
        {
            var statementData = ParseStatement();
            type = statementData.type;
            arguments = statementData.arguments;
        }
        catch (ParsingException ex)
        {
            error = ex.Message;
            result = new Statement(label, StatementType.Unknown, []);
            return false;
        }
        
        next = Peek();
        if (next is null || next.Type is TokenType.NewLine)
        {
            _pointer += 2;
            result = new Statement(label, type, arguments);
            return true;
        }
        
        error = "Expected a new line at the end of a statement";
        result = new Statement(label, type, arguments);
        return false;
    }

    private short ParseLabel()
    {
        var token = _tokens[_pointer];
        
        if (token.Type is not TokenType.Number)
        { throw new UnexpectedOrEmptyTokenException($"Label {token} should be a number"); }
        
        if (!short.TryParse(token.ToString(), out var value) || value is < 1 or > short.MaxValue)
        { throw new InvalidLabelException("Label should be more than 0 and less than 32768"); }

        return value;
    }
    
    private (StatementType type, TinyBasicToken[] arguments) ParseStatement()
    {
        TinyBasicToken token = _tokens[_pointer];
        
        string value = token.ToString();
        StatementType type = StatementType.Unknown;
        TinyBasicToken[] arguments = [];

        try
        {
            switch (value)
            {
                case "REM":
                {
                    type = StatementType.Rem;
                    arguments = ParseRem();
                    break;
                }
                case "PRINT":
                {
                    type = StatementType.Print;
                    if (Peek() is null)
                    { throw new UnexpectedOrEmptyTokenException("Expected a list of expressions after PRINT keyword"); }
                    
                    arguments = ParseExprList();
                    if (arguments.Length < 1)
                    { throw new ParsingException("Expected at least one expression or quoted string in arguments"); }
                    break;
                }
                case "IF":
                    type = StatementType.If;
                    if (Peek() is null)
                    { throw new UnexpectedOrEmptyTokenException("Expected an expression after IF keyword"); }
                    
                    arguments = ParseIf();
                    break;
                case "GOSUB":
                case "GOTO":
                {
                    type = value is "GOSUB" ? StatementType.Gosub : StatementType.Goto;
                    if (Peek() is null)
                    { throw new UnexpectedOrEmptyTokenException($"Expected a number after {value} keyword"); }

                    arguments = ParseGotoGosub();
                    break;
                }
                case "INPUT":
                {
                    type = StatementType.Input;
                    if (Peek() is null)
                    { throw new UnexpectedOrEmptyTokenException("Expected a list of variables after INPUT keyword"); }

                    arguments = ParseVarList();
                    if (arguments.Length < 1)
                    { throw new ParsingException("Expected at least one variable in arguments"); }
                    break;
                }
                case "LET":
                {
                    type = StatementType.Let;
                    if (Peek() is null)
                    { throw new UnexpectedOrEmptyTokenException("Expected a variable name after LET keyword"); }

                    arguments = ParseLet();
                    break;
                }
                case "RETURN":
                {
                    type = StatementType.Return;
                    break;
                }
                case "CLEAR":
                {
                    type = StatementType.Clear;
                    break;
                }
                case "LIST":
                {
                    type = StatementType.List;
                    break;
                }
                case "RUN":
                {
                    type = StatementType.Run;
                    break;
                }
                case "END":
                {
                    type = StatementType.End;
                    break;
                }
                default:
                { throw new UnexpectedOrEmptyTokenException($"Unexpected keyword {token}"); }
            }
        }
        catch(ParsingException ex)
        {
            if (type is StatementType.Unknown)
            { throw; }

            throw new ParsingException($"Error while parsing {type.ToString().ToUpper()} statement:\n >{ex.Message}");
        }
        
        
        return (type, arguments);
    }

    private TinyBasicToken[] ParseGotoGosub()
    {
        ++_pointer;
        ParseLabel();
        return [_tokens[_pointer]];
    }

    private TinyBasicToken[] ParseRem()
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
        if (builder.Length > 0)
        { builder.Remove(builder.Length - 1, 1); }
        return [new ValueToken(TokenType.QuotedString, builder.ToString())];
    }
    
    private TinyBasicToken[] ParseIf()
    {
        List<TinyBasicToken> arguments = [];
        var expressionSpan = ExpressionParser.SelectExpressionFromLine(_tokens, _pointer + 1);
        if (expressionSpan.Length == 0)
        { throw new UnexpectedOrEmptyTokenException($"Expected an expression, got: {_tokens[_pointer + 1]}"); }
        ExpressionToken expression;
        try
        { expression = ExpressionParser.ParseExpression(expressionSpan); }
        catch (ParsingException ex)
        { throw new ParsingException($"Failed to parse expression:\n >{ex.Message}"); }
        _pointer += expressionSpan.Length;
        arguments.Add(expression);

        TinyBasicToken? next = Peek();
        if (next?.Type != TokenType.Operator)
        { throw new UnexpectedOrEmptyTokenException("Expected a comparison operator after expression"); }
        arguments.Add(next);
        
        ++_pointer;
        next = Peek();
        if (next is null)
        { throw new UnexpectedOrEmptyTokenException("Expected an expression after comparison operator"); }
        
        expressionSpan = ExpressionParser.SelectExpressionFromLine(_tokens, _pointer + 1);
        if (expressionSpan.Length == 0)
        { throw new UnexpectedOrEmptyTokenException($"Expected an expression, got: {_tokens[_pointer + 1]}"); }
        try
        { expression = ExpressionParser.ParseExpression(expressionSpan); }
        catch (ParsingException ex)
        { throw new ParsingException($"Failed to parse expression:\n >{ex.Message}"); }
        _pointer += expressionSpan.Length;
        arguments.Add(expression);

        next = Peek();
        if (next?.ToString() is not "THEN")
        { throw new UnexpectedOrEmptyTokenException("Expected a THEN keyword after expression"); }
        arguments.Add(next); 
        
        ++_pointer;
        next = Peek();
        if (next is null)
        { throw new UnexpectedOrEmptyTokenException("Expected a statement after THEN keyword"); }
        
        ++_pointer;
        var nextStatementData = ParseStatement();
        var nextStatement = new Statement(null, nextStatementData.type, nextStatementData.arguments);
        arguments.Add(nextStatement);
        return arguments.ToArray();
    }
    
    private TinyBasicToken[] ParseExprList()
    {
        List<TinyBasicToken> arguments = [];
        var next = Peek();
        while (next is not null && next.Type is not TokenType.NewLine)
        {
            switch (next.Type)
            {
                case TokenType.QuotedString:
                {
                    arguments.Add(next);
                    
                    ++_pointer;
                    next = Peek();
                    if (next is not null && next.Type is not (TokenType.Comma or TokenType.NewLine))
                    { throw new UnexpectedOrEmptyTokenException("Expected a comma or end of the line after expression"); }
                    continue;
                }
                case TokenType.Comma:
                {
                    arguments.Add(next);
                    
                    ++_pointer;
                    next = Peek();
                    if (next is null || next.Type is TokenType.NewLine or TokenType.Comma)
                    { throw new UnexpectedOrEmptyTokenException("Expected a next expression after comma"); }
                    continue;
                }
                default:
                {
                    var expressionSpan = ExpressionParser.SelectExpressionFromLine(_tokens, _pointer + 1);
                    if (expressionSpan.Length == 0)
                    { throw new ParsingException($"Expected an expression, got {next}"); }
                    try
                    { 
                        var expression = ExpressionParser.ParseExpression(expressionSpan);
                        arguments.Add(expression);
                    }
                    catch (ParsingException ex)
                    { throw new ParsingException($"Error while parsing expression:\n >{ex.Message}"); }
                    
                    _pointer += expressionSpan.Length;
                    next = Peek();
                    if (next is not null && next.Type is not (TokenType.Comma or TokenType.NewLine))
                    { throw new UnexpectedOrEmptyTokenException("Expected a comma or end of the line after expression"); }
                    continue;
                } 
            }
            
        }
        
        return arguments.ToArray();
    }
    
    private TinyBasicToken[] ParseVarList()
    {
        List<TinyBasicToken> arguments = [];
        var next = Peek();
        while (next is not null && next.Type is not TokenType.NewLine)
        {
            switch (next.Type)
            {
                case TokenType.String:
                {
                    var value = next.ToString();
                    if (!char.TryParse(value, out var address) || address is < 'A' or > 'Z')
                    { throw new InvalidVariableNameException($"Expected a valid variable name, got {value}"); }
                    arguments.Add(next);

                    ++_pointer;
                    next = Peek();
                    if (next is not null && next.Type is not (TokenType.Comma or TokenType.NewLine))
                    { throw new UnexpectedOrEmptyTokenException($"Expected a comma or end of the line after variable {address}"); }
                    continue;
                }
                case TokenType.Comma:
                {
                    ++_pointer;
                    arguments.Add(next);
                    
                    next = Peek();
                    if (next is null || next.Type is TokenType.Comma or TokenType.NewLine)
                    { throw new UnexpectedOrEmptyTokenException("Expected a next variable after comma"); }
                    continue;
                }
                default:
                { throw new UnexpectedOrEmptyTokenException($"Unexpected token in variable list: {next}"); }
            }
        }
        
        return arguments.ToArray();
    }
    
    private TinyBasicToken[] ParseLet()
    {
        List<TinyBasicToken> arguments = [];
        var next = Peek();
        if (!char.TryParse(next?.ToString(), out char address) || (address is < 'A' or > 'Z'))
        { throw new InvalidVariableNameException($"Expected a valid variable name, got {next}"); }
        arguments.Add(next);
        
        ++_pointer;
        next = Peek();
        if (next is not OperatorToken op || op.OperatorType != OperatorType.Equals)
        { throw new UnexpectedOrEmptyTokenException($"Expected an assignment operator after {address} variable"); }
        arguments.Add(op);
        
        ++_pointer;
        next = Peek();
        if (next is null || next.Type is TokenType.NewLine)
        { throw new UnexpectedOrEmptyTokenException("Expected an expression after assignment operator"); }
        
        var expressionSpan = ExpressionParser.SelectExpressionFromLine(_tokens, _pointer + 1);
        if (expressionSpan.Length == 0)
        { throw new ParsingException($"Expected an expression, got {next}"); }
        try
        {
            var expression = ExpressionParser.ParseExpression(expressionSpan);
            arguments.Add(expression);
        }
        catch (ParsingException ex)
        { throw new ParsingException($"Failed to parse expression:\n >{ex.Message}"); }

        _pointer += expressionSpan.Length;
        return arguments.ToArray();
    }
    
    private TinyBasicToken? Peek() => ((_pointer + 1) < _tokens.Length) ? _tokens[_pointer + 1] : null;
    
    public bool CanReadLine() => _pointer < _tokens.Length;
}