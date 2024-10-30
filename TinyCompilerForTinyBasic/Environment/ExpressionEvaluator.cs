using TinyCompilerForTinyBasic.Tokenization;

namespace TinyCompilerForTinyBasic.Environment;

public class ExpressionEvaluator
{
    private EnvironmentMemory _memory;
    public ExpressionEvaluator(EnvironmentMemory memory) => _memory = memory;

    public short EvaluateExpression(TinyBasicToken[] expression)
    {
        int start = 0;
        short value = EvaluateExpression(expression, ref start);

        return value;
    }

    private short EvaluateExpression(TinyBasicToken[] expression, ref int start)
    {
        int value = EvaluateTerm(expression, ref start);
        
        while ((start + 1) < expression.Length)
        {
            TinyBasicToken op = expression[start + 1];
            if (op.Type is not (TBTokenType.OperatorPlus or TBTokenType.OperatorMinus))
            { break; }

            start += 2;
            int secondValue = EvaluateTerm(expression, ref start);
            value = op.Type is TBTokenType.OperatorPlus ? unchecked((short)(value + secondValue)) : unchecked((short)(value - secondValue));
        }
        return unchecked((short)value);
    }
    
    private short EvaluateTerm(TinyBasicToken[] expression, ref int start)
    {
        short value = EvaluateFactor(expression, ref start);

        while ((start + 1) < expression.Length)
        {
            TinyBasicToken op = expression[start + 1];
            if (op.Type is not (TBTokenType.OperatorDivision or TBTokenType.OperatorMultiplication))
            { break; }
            
            start += 2;
            short secondValue = EvaluateFactor(expression, ref start);
            
            if (op.Type is TBTokenType.OperatorMultiplication)
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
            case TBTokenType.OperatorMinus:
            {
                shouldNegate = !shouldNegate;
                ++start;
                goto res;
            }
            case TBTokenType.OperatorPlus:
            {
                ++start;
                goto res;
            }
            case TBTokenType.Number:
            {
                int value = int.Parse(token.ToString());
                return shouldNegate ? unchecked((short)-value) : unchecked((short)value);
            }
            case TBTokenType.String:
            {
                char address = char.Parse(token.ToString());
                short? value = _memory.ReadVariable(address);
                if (value is null)
                { throw new UnitializedVariableException($"Tried to read an uninitialized variable \"{address}\""); }

                return (short)(shouldNegate ? -value.Value : value.Value);
            }
            case TBTokenType.ParenthesisOpen:
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
}