using TinyCompilerForTinyBasic.Tokenization;

namespace TinyCompilerForTinyBasic.Parsing;

public interface IFunctionParser
{
    public FunctionToken Parse(string signature, IToken[][] arguments);
}

public class RandomParser : IFunctionParser
{
    public FunctionToken Parse(string signature, IToken[][] arguments)
    {
        if (signature != "RND")
        { throw new ArgumentException("Tried to parse RND function without RND signature"); }

        return new FunctionToken(signature, ParseArguments(arguments));
    }

    private IToken[] ParseArguments(IToken[][] args)
    {
        if (args.Length != 1)
        { throw new UnexpectedTokenException($"Expected one argument for RND function, got: {args.Length}"); }

        var parsed = ExpressionParser.ParseExpression(args[0]);
        return [parsed];
    }
}