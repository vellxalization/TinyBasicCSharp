using TinyCompilerForTinyBasic.Tokenization;

namespace TinyCompilerForTinyBasic.Tests;

public class LexerTests
{
    [Theory]
    [InlineData("PRINT \"HELLO WORLD")]
    [InlineData("PRINT \"HELLO WORLD\"\"")]
    public void LexerTestQuotationException(string input)
    {
        var lexer = new Lexer(input);
        Assert.Throws<UnmatchedQuotationException>(() => lexer.Tokenize());
    }

    [Theory]
    [InlineData("PRINT_\"HELLO WORLD\"")]
    [InlineData("PRINT\u200e\"HELLO WORLD\"")]
    public void LexerTestUnknownCharacterException(string input)
    {
        var lexer = new Lexer(input);
        Assert.Throws<UnknownCharacterException>(() => lexer.Tokenize());
    }

    [Theory]
    [InlineData("PRINT \"HELLO WORLD!\"", 0)]
    [InlineData("100 IF X <> 102 * X THEN X = 100 \n", 1)]
    [InlineData("10  0IF X<>102*  X THEN X=1    00\n", 1)]
    [InlineData("INPUT X, Y,Z", 2)]
    public void LexerTest(string input, int expectedTokenIndex)
    {
        var lexer = new Lexer(input);
        var token = lexer.Tokenize();
        var expectedTokens = GetExpectedTokens(expectedTokenIndex);
        Assert.True(token.Length == expectedTokens.Length);
        for (int i = 0; i < token.Length; ++i)
        {
            Assert.True(token[i].Type == expectedTokens[i].Type);
            if (token[i] is ValueTinyBasicToken valueToken)
            { Assert.True(valueToken.Value == ((ValueTinyBasicToken)(expectedTokens[i])).Value); }
        }
    }

    private TinyBasicToken[] GetExpectedTokens(int index)
    {
        return index switch
        {
            0 =>
            [
                new ValueTinyBasicToken(TBTokenType.String, "PRINT"),
                new ValueTinyBasicToken(TBTokenType.QuotedString, "HELLO WORLD!")
            ],
            1 =>
            [
                new ValueTinyBasicToken(TBTokenType.Number, "100"), new ValueTinyBasicToken(TBTokenType.String, "IF"), 
                new ValueTinyBasicToken(TBTokenType.String, "X"), new TinyBasicToken(TBTokenType.OperatorNotEqual), 
                new ValueTinyBasicToken(TBTokenType.Number, "102"), new TinyBasicToken(TBTokenType.OperatorMultiplication),
                new ValueTinyBasicToken(TBTokenType.String, "X"), new ValueTinyBasicToken(TBTokenType.String, "THEN"), 
                new ValueTinyBasicToken(TBTokenType.String, "X"), new TinyBasicToken(TBTokenType.OperatorEquals), 
                new ValueTinyBasicToken(TBTokenType.Number, "100"), new TinyBasicToken(TBTokenType.NewLine),
            ],
            2 =>
            [
                new ValueTinyBasicToken(TBTokenType.String, "INPUT"), 
                new ValueTinyBasicToken(TBTokenType.String, "X"), new TinyBasicToken(TBTokenType.Comma),
                new ValueTinyBasicToken(TBTokenType.String, "Y"), new TinyBasicToken(TBTokenType.Comma),
                new ValueTinyBasicToken(TBTokenType.String, "Z")
            ],
            _ => []
        };
    }
}