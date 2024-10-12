namespace TinyCompilerForTinyBasic;

public class TokenizationException : Exception
{
    public TokenizationException(string message) : base(message) {}
}

public class ParsingException : Exception
{
    public ParsingException(string message) : base(message) {}
}

public class RuntimeException : Exception
{
    public RuntimeException(string message) : base(message) {}
}