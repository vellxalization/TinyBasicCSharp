using System.Runtime.InteropServices;
using TinyCompilerForTinyBasic.Environment;
using TinyCompilerForTinyBasic.Parsing;
using TinyCompilerForTinyBasic.Tokenization;

namespace TinyCompilerForTinyBasic.Tests;

public class ExpressionEvaluatorTests
{
    [Theory]
    [InlineData("-X")]
    [InlineData("X")]
    [InlineData("5 + X")]
    [InlineData("Y + X")]
    [InlineData("Y + (X)")]
    [InlineData("Y + (-X)")]
    public void ExpressionEvaluatorUninitializedVariableException(string input)
    {
        var memory = new EnvironmentMemory();
        memory.WriteVariable(0, 'Y');

        var lexer = new Lexer(input);
        var tokens = lexer.Tokenize();
        int start = 0;
        var expression = ParsingUtils.SelectExpressionFromLine(tokens, ref start);
        var evaluator = new ExpressionEvaluator(memory);
        Assert.Throws<UnitializedVariableException>(() => evaluator.EvaluateExpression(expression.Components));
    }
    
    [Theory]
    [InlineData("10 / 0")]
    [InlineData("10 / (0)")]
    [InlineData("10 / (10 - 10)")]
    [InlineData("10 / Y")]
    [InlineData("10 / (Y / 1)")]
    public void ExpressionEvaluatorDivisionByZeroException(string input)
    {
        var memory = new EnvironmentMemory();
        memory.WriteVariable(0, 'Y');

        var lexer = new Lexer(input);
        var tokens = lexer.Tokenize();
        int start = 0;
        var expression = ParsingUtils.SelectExpressionFromLine(tokens, ref start);
        var evaluator = new ExpressionEvaluator(memory);
        Assert.Throws<DivisionByZeroException>(() => evaluator.EvaluateExpression(expression.Components));
    }

    [Theory]
    [InlineData("-4096", -4096)]
    [InlineData("15*4096", -4096)]
    [InlineData("32768/8", -4096)]
    [InlineData("30720+30720", -4096)]
    [InlineData("10 + Y", 10)]
    [InlineData("10 * Y", 0)]
    [InlineData("10 * (Y + 10)", 100)]
    [InlineData("10 * (10 * (10 + 30) / 2)", 2000)]
    public void ExpressionEvaluatorTest(string input, short expectedResult)
    {
        var memory = new EnvironmentMemory();
        memory.WriteVariable(0, 'Y');

        var lexer = new Lexer(input);
        var tokens = lexer.Tokenize();
        int start = 0;
        var expression = ParsingUtils.SelectExpressionFromLine(tokens, ref start);
        var evaluator = new ExpressionEvaluator(memory);
        short result = evaluator.EvaluateExpression(expression.Components);
        Assert.Equal(expectedResult, result);
    }
}