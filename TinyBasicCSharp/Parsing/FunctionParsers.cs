using TinyBasicCSharp.Tokenization;

namespace TinyBasicCSharp.Parsing;

/// <summary>
/// An abstract function parser
/// </summary>
public interface IFunctionParser
{
    /// <summary>
    /// Tries to parse a function and it's arguments
    /// </summary>
    /// <param name="signature">Function name</param>
    /// <param name="arguments">Function arguments</param>
    /// <returns>Function token if parsing is successful</returns>
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