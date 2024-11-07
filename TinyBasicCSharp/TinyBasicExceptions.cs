namespace TinyCompilerForTinyBasic;

public class TokenizationException : Exception
{
    public TokenizationException(string message) : base(message) {}
}

public class UnmatchedQuotationException : TokenizationException
{
    public UnmatchedQuotationException(string message) : base(message) {}
}

// public class UnknownCharacterException : TokenizationException
// {
//     public UnknownCharacterException(string message) : base(message) {}
// }

public class ParsingException : Exception
{
    public ParsingException(string message) : base(message) {}
}

public class EmptyExpressionException : ParsingException
{
    public EmptyExpressionException(string message) : base(message) {}
}

public class UnexpectedOrEmptyTokenException : ParsingException
{
    public UnexpectedOrEmptyTokenException(string message) : base(message) {}
}

public class InvalidVariableNameException : ParsingException
{
    public InvalidVariableNameException(string message) : base(message) {}
}

public class RuntimeException : Exception
{
    public RuntimeException(string message) : base(message) {}
}

public class UnitializedVariableException : RuntimeException
{
    public UnitializedVariableException(string message) : base(message) {}
}

public class DivisionByZeroException : RuntimeException
{
    public DivisionByZeroException(string message) : base(message) {}
}