// namespace TinyCompilerForTinyBasic.Tests;
//
// public class LexerTest
// {
//     [Theory]
//     [InlineData("LET A = 12 \n IF A <= 12 THEN PRINT \"Hello World!\" \n A = 15", 0, false)]
//     [InlineData(@"LETA=12IFA=12THENPRINT""Hello World!""A=15", 1, false)]
//     [InlineData(@"1 2 3 LET A = 1 2 IF A = 12 THEN PRINT ""Hello World!"" A = 15", 2, false)]
//     [InlineData(@"LET A = 12} IF A = 12 THEN PRINT ""Hello World!"" A = 15", 0, true)]
//     [InlineData(@"LET A = 12 IF A = 12 THEN PRINT ""Hello World! A = 15", 0, true)]
//     public void LexerTesting(string sourceCode, int index, bool shouldThrowException)
//     {
//         var lexer = new Lexer(sourceCode);
//         if (shouldThrowException)
//         {
//             Assert.Throws<Exception>(() => lexer.Tokenize());
//             return;
//         }
//         
//         var tokens = lexer.Tokenize();
//         var expectedTokens = ExpectedTokens.GetTokens(index);
//         for (int i = 0; i < tokens.Length; ++i)
//         {
//             Assert.Equal(tokens[i].Type, expectedTokens[i].Type);
//             Assert.Equal(tokens[i].Value, expectedTokens[i].Value);
//         }
//     }
//
//     public static class ExpectedTokens
//     {
//         public static TinyBasicToken[] GetTokens(int index) => Tokens[index];
//         private static TinyBasicToken[][] Tokens = new TinyBasicToken[][]
//         {
//             new []
//             {
//                 new TinyBasicToken() { Type = TBTokenType.String, Value = "LET"},
//                 new TinyBasicToken() { Type = TBTokenType.String, Value = "A"},
//                 new TinyBasicToken() { Type = TBTokenType.Operator, Value = "="},
//                 new TinyBasicToken() { Type = TBTokenType.Number, Value = "12"},
//                 new TinyBasicToken() { Type = TBTokenType.NewLine, Value = "\n"},
//                 new TinyBasicToken() { Type = TBTokenType.String, Value = "IF"},
//                 new TinyBasicToken() { Type = TBTokenType.String, Value = "A"},
//                 new TinyBasicToken() { Type = TBTokenType.Operator, Value = "<="},
//                 new TinyBasicToken() { Type = TBTokenType.Number, Value = "12"},
//                 new TinyBasicToken() { Type = TBTokenType.String, Value = "THEN"},
//                 new TinyBasicToken() { Type = TBTokenType.String, Value = "PRINT"},
//                 new TinyBasicToken() { Type = TBTokenType.QuotedString, Value = @"""Hello World!"""},
//                 new TinyBasicToken() { Type = TBTokenType.NewLine, Value = "\n"},
//                 new TinyBasicToken() { Type = TBTokenType.String, Value = "A"},
//                 new TinyBasicToken() { Type = TBTokenType.Operator, Value = "="},
//                 new TinyBasicToken() { Type = TBTokenType.Number, Value = "15"},
//             },
//             new []
//             {
//                 new TinyBasicToken(TBTokenType.String, "LETA"),
//                 new TinyBasicToken(TBTokenType.Operator, "="),
//                 new TinyBasicToken(TBTokenType.Number, "12"),
//                 new TinyBasicToken(TBTokenType.String, "IFA"),
//                 new TinyBasicToken(TBTokenType.Operator, "="),
//                 new TinyBasicToken(TBTokenType.Number, "12"),
//                 new TinyBasicToken(TBTokenType.String, "THENPRINT"),
//                 new TinyBasicToken(TBTokenType.QuotedString, @"""Hello World!"""),
//                 new TinyBasicToken(TBTokenType.String, "A"),
//                 new TinyBasicToken(TBTokenType.Operator, "="),
//                 new TinyBasicToken(TBTokenType.Number, "15"),
//             },
//             new []
//             {
//                 new TinyBasicToken() { Type = TBTokenType.Number, Value = "1 2 3"},
//                 new TinyBasicToken() { Type = TBTokenType.String, Value = "LET"},
//                 new TinyBasicToken() { Type = TBTokenType.String, Value = "A"},
//                 new TinyBasicToken() { Type = TBTokenType.Operator, Value = "="},
//                 new TinyBasicToken() { Type = TBTokenType.Number, Value = "1 2"},
//                 new TinyBasicToken() { Type = TBTokenType.String, Value = "IF"},
//                 new TinyBasicToken() { Type = TBTokenType.String, Value = "A"},
//                 new TinyBasicToken() { Type = TBTokenType.Operator, Value = "="},
//                 new TinyBasicToken() { Type = TBTokenType.Number, Value = "12"},
//                 new TinyBasicToken() { Type = TBTokenType.String, Value = "THEN"},
//                 new TinyBasicToken() { Type = TBTokenType.String, Value = "PRINT"},
//                 new TinyBasicToken() { Type = TBTokenType.QuotedString, Value = @"""Hello World!"""},
//                 new TinyBasicToken() { Type = TBTokenType.String, Value = "A"},
//                 new TinyBasicToken() { Type = TBTokenType.Operator, Value = "="},
//                 new TinyBasicToken() { Type = TBTokenType.Number, Value = "15"},
//             },
//             new []
//             {
//                 new TinyBasicToken() { Type = TBTokenType.String, Value = "LET"},
//                 new TinyBasicToken() { Type = TBTokenType.String, Value = "A"},
//                 new TinyBasicToken() { Type = TBTokenType.Operator, Value = "="},
//                 new TinyBasicToken() { Type = TBTokenType.Operator, Value = "-"},
//                 new TinyBasicToken() { Type = TBTokenType.Number, Value = "4096"},
//                 new TinyBasicToken() { Type = TBTokenType.NewLine, Value = "\n"},
//                 new TinyBasicToken() { Type = TBTokenType.String, Value = "LET"},
//                 new TinyBasicToken() { Type = TBTokenType.String, Value = "B"},
//                 new TinyBasicToken() { Type = TBTokenType.Operator, Value = "="},
//                 new TinyBasicToken() { Type = TBTokenType.Number, Value = "15"},
//                 new TinyBasicToken() { Type = TBTokenType.Operator, Value = "*"},
//                 new TinyBasicToken() { Type = TBTokenType.Number, Value = "4096"},
//                 new TinyBasicToken() { Type = TBTokenType.NewLine, Value = "\n"},
//                 new TinyBasicToken() { Type = TBTokenType.String, Value = "LET"},
//                 new TinyBasicToken() { Type = TBTokenType.String, Value = "C"},
//                 new TinyBasicToken() { Type = TBTokenType.Operator, Value = "="},
//                 new TinyBasicToken() { Type = TBTokenType.Number, Value = "32768"},
//                 new TinyBasicToken() { Type = TBTokenType.Operator, Value = "/"},
//                 new TinyBasicToken() { Type = TBTokenType.Number, Value = "8"},
//                 new TinyBasicToken() { Type = TBTokenType.NewLine, Value = "\n"},
//                 new TinyBasicToken() { Type = TBTokenType.String, Value = "LET"},
//                 new TinyBasicToken() { Type = TBTokenType.String, Value = "D"},
//                 new TinyBasicToken() { Type = TBTokenType.Operator, Value = "="},
//                 new TinyBasicToken() { Type = TBTokenType.Number, Value = "30720"},
//                 new TinyBasicToken() { Type = TBTokenType.Operator, Value = "+"},
//                 new TinyBasicToken() { Type = TBTokenType.Number, Value = "30720"},
//             },
//         };
//     }
//     
//     // -128/(-32768+(I*1))
// }