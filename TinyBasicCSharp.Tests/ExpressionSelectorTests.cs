// using TinyCompilerForTinyBasic.Parsing;
// using TinyCompilerForTinyBasic.Tokenization;
//
// namespace TinyCompilerForTinyBasic.Tests;
//
// public class ExpressionSelectorTests
// {
//     [Theory]
//     [InlineData(0)]
//     [InlineData(1)]
//     [InlineData(2)]
//     [InlineData(3)]
//     [InlineData(4)]
//     public void ExpressionSelectorTest(int index)
//     {
//         var line = GetLine(index);
//         var expectedExpression = GetExpectedExpression(index).Components;
//
//         var actualExpression = ExpressionParser.SelectExpressionFromLine(line, 0);
//         Assert.True(actualExpression.Length == expectedExpression.Length);
//         for (int i = 0; i < actualExpression.Length; ++i)
//         {
//             Assert.True(actualExpression[i].Type == expectedExpression[i].Type);
//             
//             if (actualExpression[i] is ValueToken valueToken)
//             { Assert.True(valueToken.Value == ((ValueToken)(expectedExpression[i])).Value); }
//         }
//     }
//
//     private TinyBasicToken[] GetLine(int index)
//     {
//         return index switch
//         {
//             0 => [new ValueToken(TokenType.String, "X")],
//             1 => [new ValueToken(TokenType.String, "X"), new TinyBasicToken(TokenType.OperatorPlus),
//                 new ValueToken(TokenType.String, "LET"), new TinyBasicToken(TokenType.OperatorMultiplication)],
//             2 => [new ValueToken(TokenType.String, "X"), new TinyBasicToken(TokenType.OperatorPlus), new ValueToken(TokenType.Number, "10"),
//             new TinyBasicToken(TokenType.OperatorMultiplication), new TinyBasicToken(TokenType.ParenthesisOpen), new TinyBasicToken(TokenType.ParenthesisOpen),
//             new ValueToken(TokenType.Number, "2"), new TinyBasicToken(TokenType.ParenthesisClose), new TinyBasicToken(TokenType.OperatorMinus), 
//             new ValueToken(TokenType.String, "Y"), new TinyBasicToken(TokenType.ParenthesisClose)],
//             3 => [new ValueToken(TokenType.String, "X"), new TinyBasicToken(TokenType.OperatorPlus), new ValueToken(TokenType.Number, "10"),
//                 new TinyBasicToken(TokenType.OperatorMultiplication), new TinyBasicToken(TokenType.ParenthesisOpen), new TinyBasicToken(TokenType.ParenthesisOpen),
//                 new ValueToken(TokenType.Number, "2"), new TinyBasicToken(TokenType.ParenthesisClose), new TinyBasicToken(TokenType.OperatorMinus), 
//                 new ValueToken(TokenType.String, "Yz"), new TinyBasicToken(TokenType.ParenthesisClose)],
//             4 => [new ValueToken(TokenType.String, "LET")],
//             _ => []
//         };
//     }
//     
//     private ExpressionToken GetExpectedExpression(int index)
//     {
//         return index switch
//         {
//             0 => new ExpressionToken()
//             { Components = [new ValueToken(TokenType.String, "X")] },
//             1 => new ExpressionToken()
//             { Components = [new ValueToken(TokenType.String, "X"), new TinyBasicToken(TokenType.OperatorPlus)] },
//             2 => new ExpressionToken()
//             {
//                 Components = 
//                 [
//                     new ValueToken(TokenType.String, "X"), new TinyBasicToken(TokenType.OperatorPlus), new ValueToken(TokenType.Number, "10"),
//                     new TinyBasicToken(TokenType.OperatorMultiplication), new TinyBasicToken(TokenType.ParenthesisOpen), new TinyBasicToken(TokenType.ParenthesisOpen),
//                     new ValueToken(TokenType.Number, "2"), new TinyBasicToken(TokenType.ParenthesisClose), new TinyBasicToken(TokenType.OperatorMinus), 
//                     new ValueToken(TokenType.String, "Y"), new TinyBasicToken(TokenType.ParenthesisClose)
//                 ]
//             },
//             3 => new ExpressionToken()
//             {
//                 Components = 
//                 [
//                     new ValueToken(TokenType.String, "X"), new TinyBasicToken(TokenType.OperatorPlus), new ValueToken(TokenType.Number, "10"),
//                     new TinyBasicToken(TokenType.OperatorMultiplication), new TinyBasicToken(TokenType.ParenthesisOpen), new TinyBasicToken(TokenType.ParenthesisOpen),
//                     new ValueToken(TokenType.Number, "2"), new TinyBasicToken(TokenType.ParenthesisClose), new TinyBasicToken(TokenType.OperatorMinus)
//                 ]
//             },
//             _ => new ExpressionToken()
//         };
//     }
// }