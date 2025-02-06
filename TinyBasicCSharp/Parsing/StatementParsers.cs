using System.Diagnostics;
using TinyBasicCSharp.Tokenization;

namespace TinyBasicCSharp.Parsing;

public interface IStatementParser
{
    public Statement Parse(Span<IToken> line);
}

public class LetParser : IStatementParser
{
    public Statement Parse(Span<IToken> line)
    {
        if (line[0] is not WordToken { Value: "LET" })
        { throw new ArgumentException("Tried to parse LET statement without a LET keyword"); }

        var index = 1;
        var reachedEnd = index >= line.Length;
        if (reachedEnd
            || line[index] is not WordToken addressToken 
            || !char.TryParse(addressToken.Value, out var address) 
            || address is < 'A' or > 'Z')
        { throw new UnexpectedTokenException($"Expected a valid variable name after LET keyword, got: {(reachedEnd ? 
                "nothing"
                : line[index])}"); }
        
        ++index;
        reachedEnd = index >= line.Length;
        if (reachedEnd || line[index] is not OperatorToken { Type:OperatorType.Equals } assignmentToken)
        { throw new UnexpectedTokenException($"Expected an assignment operator, got: {(reachedEnd 
            ? "nothing"
            : line[index])}"); }
        ++index;
        
        if (index >= line.Length)
        { throw new UnexpectedTokenException("Expected an expression after assignment operator, got nothing"); }

        var exprSpan = ExpressionParser.SelectExpressionFromLine(line, index);
        if (exprSpan.Length == 0)
        { throw new UnexpectedTokenException($"Expected an expression after assignment operator, got: {line[index]}"); }
        
        index += exprSpan.Length;
        var expression = ExpressionParser.ParseExpression(exprSpan);
        if (index >= line.Length || line[index] is ServiceToken { Type: ServiceType.Newline })
        { return new Statement(StatementType.Let, [addressToken, assignmentToken, expression], null); }
        
        throw new UnexpectedTokenException($"Expected a newline or EOF at the end the statement, got: {line[index]}"); 
    }
}

public class InputParser : IStatementParser
{
    public Statement Parse(Span<IToken> line)
    {
        if (line[0] is not WordToken { Value: "INPUT" })
        { throw new ArgumentException("Tried to parse INPUT statement without a INPUT keyword"); }

        var index = 1;
        if (index >= line.Length)
        { throw new UnexpectedTokenException("Expected at least one argument"); }
        while (true)
        {
            if (index >= line.Length) // will be possibly true only after first iteration
            { throw new UnexpectedTokenException("Expected next argument after comma, got nothing"); }
            
            if (line[index] is not WordToken addressToken 
                || !char.TryParse(addressToken.Value, out var address) 
                || address is < 'A' or > 'Z')
            { throw new UnexpectedTokenException($"Expected a valid variable name keyword, got: {line[index]}");}
            
            ++index;
            if (index >= line.Length)
            { return new Statement(StatementType.Input, line[1..index].ToArray(), null); }
            
            if (line[index] is not ServiceToken { Type: ServiceType.Newline or ServiceType.Comma} service)
            { throw new UnexpectedTokenException($"Expected a newline, comma or EOF, got: {line[index]}"); }

            if (service.Type == ServiceType.Comma)
            { ++index; }
            else // newline
            { return new Statement(StatementType.Input, line[1..index].ToArray(), null); }
        }
    }
}

public class RemParser : IStatementParser
{
    public Statement Parse(Span<IToken> line)
    {
        if (line[0] is not WordToken { Value: "REM" })
        { throw new ArgumentException("Tried to parse REM statement without a REM keyword"); }
        
        return line.Length == 1 
            ? new Statement(StatementType.Rem, [], null) 
            : new Statement(StatementType.Rem, (line[^1] is ServiceToken { Type: ServiceType.Newline }
                    ? line[1..^1].ToArray()
                    : line[1..].ToArray()), null);
    }
}

public class JumpParser : IStatementParser
{
    public Statement Parse(Span<IToken> line)
    {
        if (line[0] is not WordToken { Value: "GOTO" or "GOSUB" } keyword)
        { throw new ArgumentException("Tried to parse JUMP statement without a GOTO or GOSUB keywords"); }
        
        if (line.Length < 2 || line[1] is not NumberToken label)
        { throw new UnexpectedTokenException($"Expected a number label, got: {(line.Length < 2 
            ? "nothing" 
            : line[1])}"); }
        
        if (label.Value is < 0 or > short.MaxValue)
        { throw new InvalidLabelException(label.Value); }
        
        if (line.Length < 3 || line[2] is ServiceToken { Type: ServiceType.Newline })
        { return new Statement(keyword.Value == "GOTO" ? StatementType.Goto : StatementType.Gosub, [label], null); }

        throw new UnexpectedTokenException($"Expected a newline or EOF at the end, got: {line[2]}");
    }
}

public class IfParser : IStatementParser
{
    public Statement Parse(Span<IToken> line)
    {
        if (line[0] is not WordToken { Value: "IF" })
        { throw new ArgumentException("Tried to parse IF statement without a IF keyword"); }

        var index = 1;
        if (index >= line.Length)
        { throw new UnexpectedTokenException("Expected an expression after IF, got nothing"); }
        
        var exprSpan = ExpressionParser.SelectExpressionFromLine(line, index);
        if (exprSpan.Length == 0)
        { throw new UnexpectedTokenException($"Expected an expression after IF, got: {line[index]}"); }
        
        var expressionA = ExpressionParser.ParseExpression(exprSpan);
        index += exprSpan.Length;
        var reachedEnd = index >= line.Length;
        if (reachedEnd || line[index] is not OperatorToken { Type:OperatorType.GreaterThan or OperatorType.GreaterThanOrEqual
                or OperatorType.LessThan or OperatorType.LessThanOrEqual or OperatorType.Equals or OperatorType.NotEqual } comparisonOperator)
        { throw new UnexpectedTokenException($"Expected a comparison operator, got: {(reachedEnd 
            ? "nothing"
            : line[index])}"); }

        ++index;
        exprSpan = ExpressionParser.SelectExpressionFromLine(line, index);
        if (exprSpan.Length == 0)
        { throw new UnexpectedTokenException("Expected an expression after comparison operator, got nothing"); }
        
        var expressionB = ExpressionParser.ParseExpression(exprSpan);
        index += exprSpan.Length;
        reachedEnd = index >= line.Length;
        if (reachedEnd || line[index] is not WordToken { Value: "THEN" } thenKeyword)
        { throw new UnreachableException($"Expected a THEN keyword, got: {(reachedEnd 
            ? "nothing"
            : line[index])}"); }

        ++index;
        if (index >= line.Length)
        { throw new UnexpectedTokenException("Expected a statement after THEN keyword, got nothing"); }

        var statement = line[index..];
        var parsed = Parser.ParseStatement(statement);
        index += statement.Length;
        if (index >= line.Length || line[index] is ServiceToken { Type: ServiceType.Newline })
        { return new Statement(StatementType.If, [expressionA, comparisonOperator, expressionB, thenKeyword, parsed], null); }
        
        throw new UnexpectedTokenException($"Expected a newline or EOF at the end, got: {line[index]}");
    }
}

public class PrintParser : IStatementParser
{
    public Statement Parse(Span<IToken> line)
    {
        if (line[0] is not WordToken { Value: "PRINT" })
        { throw new ArgumentException("Tried to parse PRINT statement without a PRINT keyword"); }
        
        var index = 1;
        if (index >= line.Length)
        { throw new UnexpectedTokenException("Expected at least one argument"); }

        var args = new List<IToken>(line.Length);
        while (true)
        {
            if (line[index] is QuotedStringToken)
            {
                args.Add(line[index]);
                ++index;
            }
            else
            {
                var exprSpan = ExpressionParser.SelectExpressionFromLine(line, index);
                if (exprSpan.Length == 0)
                { throw new UnexpectedTokenException($"Expected a quoted string or an expression, got: {line[index]}"); }
                
                args.Add(ExpressionParser.ParseExpression(exprSpan));
                index += exprSpan.Length;
            }
            if (index >= line.Length)
            { break; }
            
            if (line[index] is not ServiceToken { Type: ServiceType.Newline or ServiceType.Comma } service)
            { throw new UnexpectedTokenException($"Expected a newline, EOF or a comma, got: {line[index]}"); }

            if (service.Type == ServiceType.Comma)
            {
                args.Add(line[index]);
                ++index;
            }
            else // newline
            { break; }
        }
        return new Statement(StatementType.Print, args.ToArray(), null);
    }
}

public class SingleWordParser : IStatementParser
{
    public Statement Parse(Span<IToken> line)
    {
        if (!Enum.TryParse(line[0].ToString(), true, out StatementType statement))
        { throw new UnreachableException($"Unrecognized statement: {statement}"); }
        
        if (line.Length == 1 || line[1] is ServiceToken { Type: ServiceType.Newline })
        { return new Statement(statement, [], null); }
        
        throw new UnexpectedTokenException($"Expected a newline or EOF, got: {line[1]}");
    }
}
