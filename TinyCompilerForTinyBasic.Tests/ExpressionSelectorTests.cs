using TinyCompilerForTinyBasic.Parsing;
using TinyCompilerForTinyBasic.Tokenization;

namespace TinyCompilerForTinyBasic.Tests;

public class ExpressionSelectorTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void ExpressionSelectorTest(int index)
    {
        var line = GetLine(index);
        var expectedExpression = GetExpectedExpression(index).Components;
        
        int i = 0;
        var actualExpression = ParsingUtils.SelectExpressionFromLine(line, ref i).Components;
        Assert.True(actualExpression.Length == expectedExpression.Length);
        for (i = 0; i < actualExpression.Length; ++i)
        {
            Assert.True(actualExpression[i].Type == expectedExpression[i].Type);
            
            if (actualExpression[i] is ValueTinyBasicToken valueToken)
            { Assert.True(valueToken.Value == ((ValueTinyBasicToken)(expectedExpression[i])).Value); }
        }
    }

    private TinyBasicToken[] GetLine(int index)
    {
        return index switch
        {
            0 => [new ValueTinyBasicToken(TBTokenType.String, "X")],
            1 => [new ValueTinyBasicToken(TBTokenType.String, "X"), new TinyBasicToken(TBTokenType.OperatorPlus),
                new ValueTinyBasicToken(TBTokenType.String, "LET"), new TinyBasicToken(TBTokenType.OperatorMultiplication)],
            2 => [new ValueTinyBasicToken(TBTokenType.String, "X"), new TinyBasicToken(TBTokenType.OperatorPlus), new ValueTinyBasicToken(TBTokenType.Number, "10"),
            new TinyBasicToken(TBTokenType.OperatorMultiplication), new TinyBasicToken(TBTokenType.ParenthesisOpen), new TinyBasicToken(TBTokenType.ParenthesisOpen),
            new ValueTinyBasicToken(TBTokenType.Number, "2"), new TinyBasicToken(TBTokenType.ParenthesisClose), new TinyBasicToken(TBTokenType.OperatorMinus), 
            new ValueTinyBasicToken(TBTokenType.String, "Y"), new TinyBasicToken(TBTokenType.ParenthesisClose)],
            3 => [new ValueTinyBasicToken(TBTokenType.String, "X"), new TinyBasicToken(TBTokenType.OperatorPlus), new ValueTinyBasicToken(TBTokenType.Number, "10"),
                new TinyBasicToken(TBTokenType.OperatorMultiplication), new TinyBasicToken(TBTokenType.ParenthesisOpen), new TinyBasicToken(TBTokenType.ParenthesisOpen),
                new ValueTinyBasicToken(TBTokenType.Number, "2"), new TinyBasicToken(TBTokenType.ParenthesisClose), new TinyBasicToken(TBTokenType.OperatorMinus), 
                new ValueTinyBasicToken(TBTokenType.String, "Yz"), new TinyBasicToken(TBTokenType.ParenthesisClose)],
            4 => [new ValueTinyBasicToken(TBTokenType.String, "LET")],
            _ => []
        };
    }
    
    private ExpressionTinyBasicToken GetExpectedExpression(int index)
    {
        return index switch
        {
            0 => new ExpressionTinyBasicToken()
            { Components = [new ValueTinyBasicToken(TBTokenType.String, "X")] },
            1 => new ExpressionTinyBasicToken()
            { Components = [new ValueTinyBasicToken(TBTokenType.String, "X"), new TinyBasicToken(TBTokenType.OperatorPlus)] },
            2 => new ExpressionTinyBasicToken()
            {
                Components = 
                [
                    new ValueTinyBasicToken(TBTokenType.String, "X"), new TinyBasicToken(TBTokenType.OperatorPlus), new ValueTinyBasicToken(TBTokenType.Number, "10"),
                    new TinyBasicToken(TBTokenType.OperatorMultiplication), new TinyBasicToken(TBTokenType.ParenthesisOpen), new TinyBasicToken(TBTokenType.ParenthesisOpen),
                    new ValueTinyBasicToken(TBTokenType.Number, "2"), new TinyBasicToken(TBTokenType.ParenthesisClose), new TinyBasicToken(TBTokenType.OperatorMinus), 
                    new ValueTinyBasicToken(TBTokenType.String, "Y"), new TinyBasicToken(TBTokenType.ParenthesisClose)
                ]
            },
            3 => new ExpressionTinyBasicToken()
            {
                Components = 
                [
                    new ValueTinyBasicToken(TBTokenType.String, "X"), new TinyBasicToken(TBTokenType.OperatorPlus), new ValueTinyBasicToken(TBTokenType.Number, "10"),
                    new TinyBasicToken(TBTokenType.OperatorMultiplication), new TinyBasicToken(TBTokenType.ParenthesisOpen), new TinyBasicToken(TBTokenType.ParenthesisOpen),
                    new ValueTinyBasicToken(TBTokenType.Number, "2"), new TinyBasicToken(TBTokenType.ParenthesisClose), new TinyBasicToken(TBTokenType.OperatorMinus)
                ]
            },
            _ => new ExpressionTinyBasicToken()
        };
    }
}