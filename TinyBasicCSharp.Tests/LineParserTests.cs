using TinyCompilerForTinyBasic.Parsing;
using TinyCompilerForTinyBasic.Tokenization;

namespace TinyCompilerForTinyBasic.Tests;

public class LineParserTests
{
    [Theory]
    [InlineData("100 let X = 100")]
    [InlineData("830921839 LET X = 0")]
    [InlineData("100 IF ")]
    [InlineData("100 IF (X + 10) ")]
    [InlineData("100 IF (X + 10) PRINT")]
    [InlineData("100 IF (X + 10) <> (X + 20)")]
    [InlineData("100 IF (X + 10) <> (X + 20) PRINT \"HELLO\"")]
    [InlineData("100 IF (X + 10) <> (X + 20) THEN print \"HELLO\"")]
    [InlineData("100 IF (X + 10) <> (X + 20) THEN 101 PRINT \"HELLO\"")]
    [InlineData("100 IF (X + 10) <> (X + 20) THEN ")]
    [InlineData("100 PRINT ")]
    [InlineData("100 PRINT ,")]
    [InlineData("100 PRINT 100, LET,")]
    [InlineData("100 PRINT 100,\"HELLO\",")]
    [InlineData("100 INPUT ")]
    [InlineData("100 INPUT 100")]
    [InlineData("100 INPUT -")]
    [InlineData("100 INPUT ,")]
    [InlineData("100 INPUT X,")]
    [InlineData("100 INPUT X,Xyz")]
    [InlineData("100 LET")]
    [InlineData("100 LET 100")]
    [InlineData("100 LET x")]
    [InlineData("100 LET X <>")]
    [InlineData("100 LET X \"HELLO\"")]
    [InlineData("100 LET X = \"HELLO\"")]
    [InlineData("100 LET X = 100,")]
    [InlineData("100 GOTO <>")]
    [InlineData("100 GOTO \"HELLO\"")]
    [InlineData("100 GOTO GOSUB")]
    [InlineData("100 PRINT X 101 PRINT Y")]
    public void LineParserBadSyntaxTest(string input)
    {
        var lexer = new Lexer(input);
        var parser = new LineParser(lexer.Tokenize());
        Assert.True(!parser.ParseLine(out _, out _));
    }

    [Theory]
    [InlineData("PRINT 10")]
    [InlineData("PRINT \"HELLO\"")]
    [InlineData("PRINT 10,\"HELLO 13\", \"13\", X, \"214\"")]
    [InlineData("IF X = 13 THEN PRINT \"HELLO\"")]
    [InlineData("IF X = 10 THEN IF Y <> 15 THEN IF Z < (10 * X + (X * 235 + (2555))) THEN PRINT \"HELLO\"")]
    [InlineData("IF (10 * X + (X * 235 + (2555))) <> (10 * X + (X * 235 + (2555))) THEN PRINT \"HELLO\"")]
    [InlineData("GOTO X")]
    [InlineData("GOTO 255")]
    [InlineData("GOTO (10 * X + (X * 235 + (2555)))")]
    [InlineData("INPUT X")]
    [InlineData("INPUT X,Y, Z")]
    [InlineData("100 LET X = 1000000000000")]
    [InlineData("100 LET X = (10 * X + (X * 235 + (2555)))")]
    [InlineData("PRINT 10,\"HELLO 13\", \"13\", X, \"214\"\n\n\nIF X = 13 THEN PRINT \"HELLO\"\n\nIF X = 10 THEN IF Y <> 15 THEN IF Z < (10 * X + (X * 235 + (2555))) THEN PRINT \"HELLO\"\n")]
    public void LineParserTest(string input)
    {
        var lexer = new Lexer(input);
        var parser = new LineParser(lexer.Tokenize());
        while (parser.CanReadLine())
        { Assert.True(parser.ParseLine(out _, out _)); }
    }
}