using TinyCompilerForTinyBasic.Tokenization;

namespace TinyCompilerForTinyBasic.Environment;

/// <summary>
/// Class for evaluating expressions
/// </summary>
public class ExpressionEvaluator
{
    private EnvironmentMemory _memory;
    public ExpressionEvaluator(EnvironmentMemory memory) => _memory = memory;

    /// <summary>
    /// Evaluates provided expression.
    /// TinyBasic works with signed short values, making it very susceptible for under- and overflows.
    /// Evaluator must share same memory with environment
    /// </summary>
    /// <param name="expression">Syntactically correct expression</param>
    /// <returns>Value of expression</returns>
    public short EvaluateExpression(ExpressionToken expression)
    {
        int start = 0;
        short value = EvaluateExpression(expression.Components, ref start);

        return value;
    }

    private short EvaluateExpression(TinyBasicToken[] expression, ref int start)
    {
        int value = EvaluateTerm(expression, ref start);
        
        while ((start + 1) < expression.Length)
        {
            TinyBasicToken op = expression[start + 1];
            if (op.Type is not (TokenType.OperatorPlus or TokenType.OperatorMinus))
            { break; }

            start += 2;
            int secondValue = EvaluateTerm(expression, ref start);
            value = op.Type is TokenType.OperatorPlus ? unchecked((short)(value + secondValue)) : unchecked((short)(value - secondValue));
        }
        return unchecked((short)value);
    }
    
    private short EvaluateTerm(TinyBasicToken[] expression, ref int start)
    {
        short value = EvaluateFactor(expression, ref start);

        while ((start + 1) < expression.Length)
        {
            TinyBasicToken op = expression[start + 1];
            if (op.Type is not (TokenType.OperatorDivision or TokenType.OperatorMultiplication))
            { break; }
            
            start += 2;
            short secondValue = EvaluateFactor(expression, ref start);
            
            if (op.Type is TokenType.OperatorMultiplication)
            { value = unchecked((short)(value * secondValue)); } 
            else
            {
                if (secondValue is 0)
                { throw new DivisionByZeroException("Tried to divide by zero"); }
                value = unchecked((short)(value / secondValue));
            }
        }
        return value;
    }
    
    private short EvaluateFactor(TinyBasicToken[] expression, ref int start)
    {
        bool shouldNegate = false;
        res:
        TinyBasicToken token = expression[start];
        switch (token.Type)
        {
            case TokenType.OperatorMinus:
            {
                shouldNegate = !shouldNegate;
                ++start;
                goto res;
            }
            case TokenType.OperatorPlus:
            {
                ++start;
                goto res;
            }
            case TokenType.Number:
            {
                int value = int.Parse(token.ToString());
                return shouldNegate ? unchecked((short)-value) : unchecked((short)value);
            }
            case TokenType.String:
            {
                char address = char.Parse(token.ToString());
                short? value = _memory.ReadVariable(address);
                if (value is null)
                { throw new UnitializedVariableException($"Tried to read an uninitialized variable \"{address}\""); }

                return (short)(shouldNegate ? -value.Value : value.Value);
            }
            case TokenType.Function:
            {
                var funcToken = (FunctionToken)token;
                switch (funcToken.Signature)
                {
                    case "RND":
                    {
                        var randomValue = EvaluateRandom(funcToken);
                        return (short)(shouldNegate ? -randomValue : randomValue);
                    }
                    default:
                    { throw new RuntimeException($"Unknown function signature: {funcToken.Signature}"); }
                }
            }
            case TokenType.ParenthesisOpen:
            {
                ++start;
                short value = EvaluateExpression(expression, ref start);
                ++start;
                return value;
            }
            default:
            { throw new RuntimeException($"Unexpected token (\"{token}\") in expression"); }
        }
    }

    private short EvaluateRandom(FunctionToken token)
    {
        var expressionArgument = (ExpressionToken)token.Arguments[0];
        short argumentValue = EvaluateExpression(expressionArgument);
        if (argumentValue <= 0)
        { throw new RuntimeException("Argument for RND function should be more than 0"); }
        
        return (short)Random.Shared.Next(0, argumentValue);
    }
}