﻿using TinyCompilerForTinyBasic.Tokenization;

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
        TinyBasicToken token = expression[start];
        bool shouldNegate = false;
        if (token.Type is TBTokenType.OperatorPlus or TBTokenType.OperatorMinus)
        {
            shouldNegate = token.Type == TBTokenType.OperatorMinus;
            ++start;
        }
        int value = EvaluateTerm(expression, ref start);
        if (shouldNegate)
        { value = -value; }

        while ((start + 1) < expression.Length)
        {
            TinyBasicToken op = expression[start + 1];
            if (op.Type is not (TBTokenType.OperatorPlus or TBTokenType.OperatorMinus))
            { break; }

            start += 2;
            int secondValue = EvaluateTerm(expression, ref start);
            value = op.Type is TBTokenType.OperatorPlus ? (value + secondValue) : (value - secondValue);
        }
        return unchecked((short)value);
    }
    
    private int EvaluateTerm(TinyBasicToken[] expression, ref int start)
    {
        int value = EvaluateFactor(expression, ref start);

        while ((start + 1) < expression.Length)
        {
            TinyBasicToken op = expression[start + 1];
            if (op.Type is not (TBTokenType.OperatorDivision or TBTokenType.OperatorMultiplication))
            { break; }
            
            start += 2;
            int secondValue = EvaluateFactor(expression, ref start);
            
            if (op.Type is TBTokenType.OperatorMultiplication)
            { value *= secondValue; } 
            else
            {
                if (secondValue is 0)
                { throw new RuntimeException("Tried to divide by zero"); }
                value /= secondValue;
            }
        }
        return value;
    }
    
    private int EvaluateFactor(TinyBasicToken[] expression, ref int start)
    {
        TinyBasicToken token = expression[start];
        switch (token.Type)
        {
            case TBTokenType.Number:
            { return int.Parse(token.ToString()); }
            case TBTokenType.String:
            {
                char address = char.Parse(token.ToString());
                short? value = _memory.ReadVariable(address);
                if (value is null)
                { throw new RuntimeException($"Tried to read an uninitialized variable \"{address}\""); }

                return value.Value;
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