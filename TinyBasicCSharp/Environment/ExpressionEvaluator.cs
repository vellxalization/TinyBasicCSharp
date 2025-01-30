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
        short value = EvaluateExpression(expression.Arguments, ref start);

        return value;
    }

    private short EvaluateExpression(IToken[] expression, ref int start)
    {
        short value = EvaluateTerm(expression, ref start);
        
        while ((start + 1) < expression.Length
               && expression[start + 1] is OperatorToken { Type: OperatorType.Plus or OperatorType.Minus } op)
        {
            start += 2;
            short secondValue = EvaluateTerm(expression, ref start);
            value = op.Type is OperatorType.Plus ? unchecked((short)(value + secondValue)) : unchecked((short)(value - secondValue));
        }
        return value;
    }
    
    private short EvaluateTerm(IToken[] expression, ref int start)
    {
        short value = EvaluateFactor(expression, ref start);

        while ((start + 1) < expression.Length 
               && expression[start + 1] is OperatorToken { Type: OperatorType.Division or OperatorType.Multiplication } op)
        {
            start += 2;
            short secondValue = EvaluateFactor(expression, ref start);
            if (op.Type is OperatorType.Multiplication)
            { value = unchecked((short)(value * secondValue)); } 
            else
            {
                if (secondValue is 0)
                { throw new DivisionByZeroException(value); }
                value = unchecked((short)(value / secondValue));
            }
        }
        return value;
    }
    
    private short EvaluateFactor(IToken[] expression, ref int start)
    {
        bool shouldNegate = false;
        var token = expression[start];
        while (token is OperatorToken { Type: (OperatorType.Plus or OperatorType.Minus) } op)
        {
            if (op.Type is OperatorType.Minus)
            { shouldNegate = !shouldNegate; }

            ++start;
            token = expression[start];
        }
        switch (token)
        {
            case NumberToken num:
            { return shouldNegate ? unchecked((short)-num.Value) : unchecked((short)num.Value); }
            case WordToken word:
            {
                char address = char.Parse(word.Value);
                short? value = _memory.ReadVariable(address);
                if (value is null)
                { throw new UninitializedVariableException(address); }

                return (short)(shouldNegate ? -value.Value : value.Value);
            }
            case FunctionToken funcToken:
            {
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
            case ServiceToken { Type: ServiceType.ParenthesisOpen }:
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
        { throw new RuntimeException($"Argument for RND function should be more than 0, got: {argumentValue}"); }
        
        return (short)Random.Shared.Next(0, argumentValue);
    }
}