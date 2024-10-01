namespace TinyCompilerForTinyBasic.Tests;

public class LexerTest
{
    [Theory]
    [InlineData(@"LET a = 12 IF a = 12 THEN PRINT ""Hello World!"" a = 15", 0, false)]
    [InlineData(@"LETa=12IFa=12THENPRINT""Hello World!""a=15", 1, false)]
    [InlineData(@"LET a = 12} IF a = 12 THEN PRINT ""Hello World!"" a = 15", 0, true)]
    [InlineData(@"LET a = 12 IF a = 12 THEN PRINT ""Hello World! a = 15", 0, true)]
    public void LexerTesting(string sourceCode, int index, bool shouldThrowException)
    {
        var lexer = new Lexer(sourceCode);
        if (shouldThrowException)
        {
            Assert.Throws<Exception>(() => lexer.Tokenize());
            return;
        }
        
        var tokens = lexer.Tokenize();
        var expectedTokens = ExpectedTokens.GetTokens(index);
        for (int i = 0; i < tokens.Length; ++i)
        {
            Assert.Equal(tokens[i].Type, expectedTokens[i].Type);
            Assert.Equal(tokens[i].Value, expectedTokens[i].Value);
        }
    }

    private static class ExpectedTokens
    {
        public static TBToken[] GetTokens(int index) => Tokens[index];
        private static TBToken[][] Tokens = new TBToken[][]
        {
            new []
            {
                new TBToken() { Type = TBTokenType.Keyword, Value = "LET"},
                new TBToken() { Type = TBTokenType.Variable, Value = "a"},
                new TBToken() { Type = TBTokenType.Operator, Value = "="},
                new TBToken() { Type = TBTokenType.Number, Value = "12"},
                new TBToken() { Type = TBTokenType.Keyword, Value = "IF"},
                new TBToken() { Type = TBTokenType.Variable, Value = "a"},
                new TBToken() { Type = TBTokenType.Operator, Value = "="},
                new TBToken() { Type = TBTokenType.Number, Value = "12"},
                new TBToken() { Type = TBTokenType.Keyword, Value = "THEN"},
                new TBToken() { Type = TBTokenType.Keyword, Value = "PRINT"},
                new TBToken() { Type = TBTokenType.String, Value = "Hello World!"},
                new TBToken() { Type = TBTokenType.Variable, Value = "a"},
                new TBToken() { Type = TBTokenType.Operator, Value = "="},
                new TBToken() { Type = TBTokenType.Number, Value = "15"},
            },
            new []
            {
                new TBToken(TBTokenType.Variable, "LETa"),
                new TBToken(TBTokenType.Operator, "="),
                new TBToken(TBTokenType.Number, "12"),
                new TBToken(TBTokenType.Variable, "IFa"),
                new TBToken(TBTokenType.Operator, "="),
                new TBToken(TBTokenType.Number, "12"),
                new TBToken(TBTokenType.Variable, "THENPRINT"),
                new TBToken(TBTokenType.String, "Hello World!"),
                new TBToken(TBTokenType.Variable, "a"),
                new TBToken(TBTokenType.Operator, "="),
                new TBToken(TBTokenType.Number, "15"),
            }
        };
    }
}