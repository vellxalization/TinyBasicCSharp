using TinyCompilerForTinyBasic.Parsing;
using TinyCompilerForTinyBasic.Tokenization;

namespace TinyCompilerForTinyBasic.Tests;

public class ExpressionParserTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(100)]
    public void ExpressionParserInvalidTokenException(int index)
    {
        var expression = GetInvalidExpression(index);
        Assert.Throws<UnexpectedOrEmptyTokenException>(() => ParsingUtils.ParseExpression(expression));
    }

    [Fact]
    public void ExpressionParserInvalidVarNameException()
    {
        var expression = GetInvalidExpression(0);
        Assert.Throws<InvalidVariableNameException>(() => ParsingUtils.ParseExpression(expression));
    }

    [Fact]
    public void ExpressionParserEmptyExpressionException()
    {
        var expression = GetInvalidExpression(-1);
        Assert.Throws<EmptyExpressionException>(() => ParsingUtils.ParseExpression(expression));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void ExpressionParserTesting(int index)
    {
        // shouldn't get any exceptions
        ParsingUtils.ParseExpression(GetValidExpression(index));
    }

    private ExpressionTinyBasicToken GetValidExpression(int index)
    {
        return index switch
        {
            0 => new ExpressionTinyBasicToken() // X
                { Components = [new ValueTinyBasicToken(TBTokenType.String, "X")] },
            1 => new ExpressionTinyBasicToken() // (X)
                { Components = [new TinyBasicToken(TBTokenType.ParenthesisOpen), new ValueTinyBasicToken(TBTokenType.String, "X"), new TinyBasicToken(TBTokenType.ParenthesisClose)] },
            2 => new ExpressionTinyBasicToken() // (-X)
                { Components = 
                [
                    new TinyBasicToken(TBTokenType.ParenthesisOpen), new TinyBasicToken(TBTokenType.OperatorMinus),
                    new ValueTinyBasicToken(TBTokenType.String, "X"), new TinyBasicToken(TBTokenType.ParenthesisClose)
                ] },
            3 => new ExpressionTinyBasicToken() // ((-X + (-100)))
            { Components = 
            [
                new TinyBasicToken(TBTokenType.ParenthesisOpen), new TinyBasicToken(TBTokenType.ParenthesisOpen), new TinyBasicToken(TBTokenType.OperatorMinus),
                new ValueTinyBasicToken(TBTokenType.String, "X"), new TinyBasicToken(TBTokenType.OperatorPlus), new TinyBasicToken(TBTokenType.ParenthesisOpen),
                new TinyBasicToken(TBTokenType.OperatorMinus), new ValueTinyBasicToken(TBTokenType.Number, "100"), new TinyBasicToken(TBTokenType.ParenthesisClose),
                new TinyBasicToken(TBTokenType.ParenthesisClose), new TinyBasicToken(TBTokenType.ParenthesisClose)
            ] },
            4 => new ExpressionTinyBasicToken() // ((-X + 100 * (100 + Y * (124 - (24)))))
            { Components = 
                [
                    new TinyBasicToken(TBTokenType.ParenthesisOpen), new TinyBasicToken(TBTokenType.ParenthesisOpen), new TinyBasicToken(TBTokenType.OperatorMinus),
                    new ValueTinyBasicToken(TBTokenType.String, "X"), new TinyBasicToken(TBTokenType.OperatorPlus),  
                    new ValueTinyBasicToken(TBTokenType.Number, "100"), new TinyBasicToken(TBTokenType.OperatorMultiplication), new TinyBasicToken(TBTokenType.ParenthesisOpen), 
                    new ValueTinyBasicToken(TBTokenType.Number, "100"), new TinyBasicToken(TBTokenType.OperatorPlus), new ValueTinyBasicToken(TBTokenType.String, "Y"),
                    new TinyBasicToken(TBTokenType.OperatorMultiplication), new TinyBasicToken(TBTokenType.ParenthesisOpen), new ValueTinyBasicToken(TBTokenType.Number, "124"),
                    new TinyBasicToken(TBTokenType.OperatorMinus), new TinyBasicToken(TBTokenType.ParenthesisOpen), new ValueTinyBasicToken(TBTokenType.Number, "24"),
                    new TinyBasicToken(TBTokenType.ParenthesisClose), new TinyBasicToken(TBTokenType.ParenthesisClose), new TinyBasicToken(TBTokenType.ParenthesisClose), 
                    new TinyBasicToken(TBTokenType.ParenthesisClose), new TinyBasicToken(TBTokenType.ParenthesisClose)
                ]
            },
            _ => new ExpressionTinyBasicToken()
        };
    }
    
    private ExpressionTinyBasicToken GetInvalidExpression(int index)
    {
        return index switch
        {
            0 => new ExpressionTinyBasicToken() // 10 + XyZ
                { Components = [new ValueTinyBasicToken(TBTokenType.Number, "10"), new TinyBasicToken(TBTokenType.OperatorPlus), new ValueTinyBasicToken(TBTokenType.String, "XyZ")]},
            1 => new ExpressionTinyBasicToken()  // ()
                { Components = [new TinyBasicToken(TBTokenType.ParenthesisOpen), new TinyBasicToken(TBTokenType.ParenthesisClose)] },
            2 => new ExpressionTinyBasicToken() // (-)
                { Components = [new TinyBasicToken(TBTokenType.ParenthesisOpen), new TinyBasicToken(TBTokenType.OperatorMinus), new TinyBasicToken(TBTokenType.ParenthesisClose)] },
            3 => new ExpressionTinyBasicToken() // (-X
                { Components = [new TinyBasicToken(TBTokenType.ParenthesisOpen), new TinyBasicToken(TBTokenType.OperatorMinus), new ValueTinyBasicToken(TBTokenType.String, "X")] },
            4 => new ExpressionTinyBasicToken() // (-X))
            {
                Components = [new TinyBasicToken(TBTokenType.ParenthesisOpen), new TinyBasicToken(TBTokenType.OperatorMinus), new ValueTinyBasicToken(TBTokenType.String, "X"),
                new TinyBasicToken(TBTokenType.ParenthesisClose), new TinyBasicToken(TBTokenType.ParenthesisClose)]
            },
            5 => new ExpressionTinyBasicToken() // (-X +
                { Components = 
                [
                    new TinyBasicToken(TBTokenType.ParenthesisOpen), new TinyBasicToken(TBTokenType.OperatorMinus), 
                    new ValueTinyBasicToken(TBTokenType.String, "X"), new TinyBasicToken(TBTokenType.OperatorPlus)
                ] },
            6 => new ExpressionTinyBasicToken() // (-X 2)
                { Components = 
                [
                    new TinyBasicToken(TBTokenType.ParenthesisOpen), new TinyBasicToken(TBTokenType.OperatorMinus), 
                    new ValueTinyBasicToken(TBTokenType.String, "X"), new ValueTinyBasicToken(TBTokenType.Number, "2"),
                    new TinyBasicToken(TBTokenType.ParenthesisClose)
                ] },
            7 => new ExpressionTinyBasicToken() // (-X <> 2)
                { Components = 
                [
                    new TinyBasicToken(TBTokenType.ParenthesisOpen), new TinyBasicToken(TBTokenType.OperatorMinus), 
                    new ValueTinyBasicToken(TBTokenType.String, "X"), new TinyBasicToken(TBTokenType.OperatorNotEqual), 
                    new ValueTinyBasicToken(TBTokenType.Number, "2"), new TinyBasicToken(TBTokenType.ParenthesisClose)
                ] },
            100 => new ExpressionTinyBasicToken() // -X + -10 //TODO: currently not supporting this kind of negation. Workaround: replace -10 with (-10)
            { Components = 
                [
                    new TinyBasicToken(TBTokenType.OperatorMinus), new ValueTinyBasicToken(TBTokenType.String, "X"), new TinyBasicToken(TBTokenType.OperatorPlus),
                    new TinyBasicToken(TBTokenType.OperatorMinus), new ValueTinyBasicToken(TBTokenType.Number, "10")
                ]
            },
            _ => new ExpressionTinyBasicToken(),
        };
    }
}