namespace TinyCompilerForTinyBasic.Tests;

public class LexerTest
{
    [Theory]
    [InlineData(@"LET A = 12 IF (A <= 12) THEN PRINT ""Hello World!"" A = 15", 0, false)]
    [InlineData(@"LETA=12IFA=12THENPRINT""Hello World!""A=15", 1, false)]
    [InlineData(@"1 2 3 LET A = 1 2 IF A = 12 THEN PRINT ""Hello World!"" A = 15", 2, false)]
    [InlineData(@"LET A = 12} IF A = 12 THEN PRINT ""Hello World!"" A = 15", 0, true)]
    [InlineData(@"LET A = 12 IF A = 12 THEN PRINT ""Hello World! A = 15", 0, true)]
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
                new TBToken() { Type = TBTokenType.String, Value = "LET"},
                new TBToken() { Type = TBTokenType.String, Value = "A"},
                new TBToken() { Type = TBTokenType.Operator, Value = "="},
                new TBToken() { Type = TBTokenType.Number, Value = "12"},
                new TBToken() { Type = TBTokenType.String, Value = "IF"},
                new TBToken() { Type = TBTokenType.Parenthesis, Value = "("},
                new TBToken() { Type = TBTokenType.String, Value = "A"},
                new TBToken() { Type = TBTokenType.Operator, Value = "<="},
                new TBToken() { Type = TBTokenType.Number, Value = "12"},
                new TBToken() { Type = TBTokenType.Parenthesis, Value = ")"},
                new TBToken() { Type = TBTokenType.String, Value = "THEN"},
                new TBToken() { Type = TBTokenType.String, Value = "PRINT"},
                new TBToken() { Type = TBTokenType.QuotedString, Value = @"""Hello World!"""},
                new TBToken() { Type = TBTokenType.String, Value = "A"},
                new TBToken() { Type = TBTokenType.Operator, Value = "="},
                new TBToken() { Type = TBTokenType.Number, Value = "15"},
            },
            new []
            {
                new TBToken(TBTokenType.String, "LETA"),
                new TBToken(TBTokenType.Operator, "="),
                new TBToken(TBTokenType.Number, "12"),
                new TBToken(TBTokenType.String, "IFA"),
                new TBToken(TBTokenType.Operator, "="),
                new TBToken(TBTokenType.Number, "12"),
                new TBToken(TBTokenType.String, "THENPRINT"),
                new TBToken(TBTokenType.QuotedString, @"""Hello World!"""),
                new TBToken(TBTokenType.String, "A"),
                new TBToken(TBTokenType.Operator, "="),
                new TBToken(TBTokenType.Number, "15"),
            },
            new []
            {
                new TBToken() { Type = TBTokenType.Number, Value = "1 2 3"},
                new TBToken() { Type = TBTokenType.String, Value = "LET"},
                new TBToken() { Type = TBTokenType.String, Value = "A"},
                new TBToken() { Type = TBTokenType.Operator, Value = "="},
                new TBToken() { Type = TBTokenType.Number, Value = "1 2"},
                new TBToken() { Type = TBTokenType.String, Value = "IF"},
                new TBToken() { Type = TBTokenType.String, Value = "A"},
                new TBToken() { Type = TBTokenType.Operator, Value = "="},
                new TBToken() { Type = TBTokenType.Number, Value = "12"},
                new TBToken() { Type = TBTokenType.String, Value = "THEN"},
                new TBToken() { Type = TBTokenType.String, Value = "PRINT"},
                new TBToken() { Type = TBTokenType.QuotedString, Value = @"""Hello World!"""},
                new TBToken() { Type = TBTokenType.String, Value = "A"},
                new TBToken() { Type = TBTokenType.Operator, Value = "="},
                new TBToken() { Type = TBTokenType.Number, Value = "15"},
            },
        };
    }
}