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
    [InlineData("PRINT \"HELLO WORLD!\"", 0)]
    [InlineData("100 IF X <> 102 * X THEN X = 100 \n", 1)]
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
            if (token[i] is ValueToken valueToken)
            { Assert.True(valueToken.Value == ((ValueToken)(expectedTokens[i])).Value); }
        }
    }

    private TinyBasicToken[] GetExpectedTokens(int index)
    {
        return index switch
        {
            0 =>
            [
                new ValueToken(TokenType.String, "PRINT"),
                new ValueToken(TokenType.QuotedString, "HELLO WORLD!")
            ],
            1 =>
            [
                new ValueToken(TokenType.Number, "100"), new ValueToken(TokenType.String, "IF"), 
                new ValueToken(TokenType.String, "X"), new TinyBasicToken(TokenType.OperatorNotEqual), 
                new ValueToken(TokenType.Number, "102"), new TinyBasicToken(TokenType.OperatorMultiplication),
                new ValueToken(TokenType.String, "X"), new ValueToken(TokenType.String, "THEN"), 
                new ValueToken(TokenType.String, "X"), new TinyBasicToken(TokenType.OperatorEquals), 
                new ValueToken(TokenType.Number, "100"), new TinyBasicToken(TokenType.NewLine),
            ],
            2 =>
            [
                new ValueToken(TokenType.String, "INPUT"), 
                new ValueToken(TokenType.String, "X"), new TinyBasicToken(TokenType.Comma),
                new ValueToken(TokenType.String, "Y"), new TinyBasicToken(TokenType.Comma),
                new ValueToken(TokenType.String, "Z")
            ],
            _ => []
        };
    }
}