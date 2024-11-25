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
    [InlineData(8)]
    public void ExpressionParserInvalidTokenException(int index)
    {
        var expression = GetInvalidExpression(index);
        Assert.Throws<ParsingException>(() => ExpressionParser.ParseExpression(expression.Components));
    }

    [Fact]
    public void ExpressionParserInvalidVarNameException()
    {
        var expression = GetInvalidExpression(0);
        Assert.Throws<ParsingException>(() => ExpressionParser.ParseExpression(expression.Components));
    }

    [Fact]
    public void ExpressionParserEmptyExpressionException()
    {
        var expression = GetInvalidExpression(-1);
        Assert.Throws<EmptyExpressionException>(() => ExpressionParser.ParseExpression(expression.Components));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void ExpressionParserTesting(int index)
    {
        // shouldn't get any exceptions
        var expression = GetValidExpression(index);
        ExpressionParser.ParseExpression(expression.Components);
    }

    private ExpressionToken GetValidExpression(int index)
    {
        return index switch
        {
            0 => new ExpressionToken() // X
                { Components = [new ValueToken(TokenType.String, "X")] },
            1 => new ExpressionToken() // (X)
                { Components = [new TinyBasicToken(TokenType.ParenthesisOpen), new ValueToken(TokenType.String, "X"), new TinyBasicToken(TokenType.ParenthesisClose)] },
            2 => new ExpressionToken() // (-X)
                { Components = 
                [
                    new TinyBasicToken(TokenType.ParenthesisOpen), new TinyBasicToken(TokenType.OperatorMinus),
                    new ValueToken(TokenType.String, "X"), new TinyBasicToken(TokenType.ParenthesisClose)
                ] },
            3 => new ExpressionToken() // (---X)
            { Components = 
            [
                new TinyBasicToken(TokenType.ParenthesisOpen), new TinyBasicToken(TokenType.OperatorMinus),
                new TinyBasicToken(TokenType.OperatorMinus), new TinyBasicToken(TokenType.OperatorMinus),
                new ValueToken(TokenType.String, "X"), new TinyBasicToken(TokenType.ParenthesisClose)
            ] },
            4 => new ExpressionToken() // ((-X + (-100)))
            { Components = 
            [
                new TinyBasicToken(TokenType.ParenthesisOpen), new TinyBasicToken(TokenType.ParenthesisOpen), new TinyBasicToken(TokenType.OperatorMinus),
                new ValueToken(TokenType.String, "X"), new TinyBasicToken(TokenType.OperatorPlus), new TinyBasicToken(TokenType.ParenthesisOpen),
                new TinyBasicToken(TokenType.OperatorMinus), new ValueToken(TokenType.Number, "100"), new TinyBasicToken(TokenType.ParenthesisClose),
                new TinyBasicToken(TokenType.ParenthesisClose), new TinyBasicToken(TokenType.ParenthesisClose)
            ] },
            5 => new ExpressionToken() // ((-X + 100 * (100 + Y * (124 - (24)))))
            { Components = 
                [
                    new TinyBasicToken(TokenType.ParenthesisOpen), new TinyBasicToken(TokenType.ParenthesisOpen), new TinyBasicToken(TokenType.OperatorMinus),
                    new ValueToken(TokenType.String, "X"), new TinyBasicToken(TokenType.OperatorPlus),  
                    new ValueToken(TokenType.Number, "100"), new TinyBasicToken(TokenType.OperatorMultiplication), new TinyBasicToken(TokenType.ParenthesisOpen), 
                    new ValueToken(TokenType.Number, "100"), new TinyBasicToken(TokenType.OperatorPlus), new ValueToken(TokenType.String, "Y"),
                    new TinyBasicToken(TokenType.OperatorMultiplication), new TinyBasicToken(TokenType.ParenthesisOpen), new ValueToken(TokenType.Number, "124"),
                    new TinyBasicToken(TokenType.OperatorMinus), new TinyBasicToken(TokenType.ParenthesisOpen), new ValueToken(TokenType.Number, "24"),
                    new TinyBasicToken(TokenType.ParenthesisClose), new TinyBasicToken(TokenType.ParenthesisClose), new TinyBasicToken(TokenType.ParenthesisClose), 
                    new TinyBasicToken(TokenType.ParenthesisClose), new TinyBasicToken(TokenType.ParenthesisClose)
                ]
            },
            6 => new ExpressionToken() // -X + -10 
            { Components = 
                [
                    new TinyBasicToken(TokenType.OperatorMinus), new ValueToken(TokenType.String, "X"), new TinyBasicToken(TokenType.OperatorPlus),
                    new TinyBasicToken(TokenType.OperatorMinus), new ValueToken(TokenType.Number, "10")
                ]
            },
            _ => new ExpressionToken()
        };
    }
    
    private ExpressionToken GetInvalidExpression(int index)
    {
        return index switch
        {
            0 => new ExpressionToken() // 10 + XyZ
                { Components = [new ValueToken(TokenType.Number, "10"), new TinyBasicToken(TokenType.OperatorPlus), new ValueToken(TokenType.String, "XyZ")]},
            1 => new ExpressionToken()  // ()
                { Components = [new TinyBasicToken(TokenType.ParenthesisOpen), new TinyBasicToken(TokenType.ParenthesisClose)] },
            2 => new ExpressionToken() // (-)
                { Components = [new TinyBasicToken(TokenType.ParenthesisOpen), new TinyBasicToken(TokenType.OperatorMinus), new TinyBasicToken(TokenType.ParenthesisClose)] },
            3 => new ExpressionToken() // (-X
                { Components = [new TinyBasicToken(TokenType.ParenthesisOpen), new TinyBasicToken(TokenType.OperatorMinus), new ValueToken(TokenType.String, "X")] },
            4 => new ExpressionToken() // (-X))
            {
                Components = [new TinyBasicToken(TokenType.ParenthesisOpen), new TinyBasicToken(TokenType.OperatorMinus), new ValueToken(TokenType.String, "X"),
                new TinyBasicToken(TokenType.ParenthesisClose), new TinyBasicToken(TokenType.ParenthesisClose)]
            },
            5 => new ExpressionToken() // (-X +
                { Components = 
                [
                    new TinyBasicToken(TokenType.ParenthesisOpen), new TinyBasicToken(TokenType.OperatorMinus), 
                    new ValueToken(TokenType.String, "X"), new TinyBasicToken(TokenType.OperatorPlus)
                ] },
            6 => new ExpressionToken() // -X - -
            { Components = 
            [
                new TinyBasicToken(TokenType.OperatorMinus), new ValueToken(TokenType.String, "X"), 
                new TinyBasicToken(TokenType.OperatorMinus), new TinyBasicToken(TokenType.OperatorMinus)
            ] },
            7 => new ExpressionToken() // (-X 2)
                { Components = 
                [
                    new TinyBasicToken(TokenType.ParenthesisOpen), new TinyBasicToken(TokenType.OperatorMinus), 
                    new ValueToken(TokenType.String, "X"), new ValueToken(TokenType.Number, "2"),
                    new TinyBasicToken(TokenType.ParenthesisClose)
                ] },
            8 => new ExpressionToken() // (-X <> 2)
                { Components = 
                [
                    new TinyBasicToken(TokenType.ParenthesisOpen), new TinyBasicToken(TokenType.OperatorMinus), 
                    new ValueToken(TokenType.String, "X"), new TinyBasicToken(TokenType.OperatorNotEqual), 
                    new ValueToken(TokenType.Number, "2"), new TinyBasicToken(TokenType.ParenthesisClose)
                ] },
            _ => new ExpressionToken(),
        };
    }
}