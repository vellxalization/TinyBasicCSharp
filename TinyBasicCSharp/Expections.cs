namespace TinyCompilerForTinyBasic;

public class TinyBasicException(string message, Exception? innerException = null) : Exception(message, innerException)
{
    public void PrintException()
    {
        Console.WriteLine(Message);
        var next = InnerException;
        while (next != null)
        {
            Console.WriteLine($" >{next.Message}");
            next = next.InnerException;  
        }
    }
};

public class TokenizationException(string message, Exception? innerException = null) : TinyBasicException(message, innerException);

public class ParsingException(string message, Exception? innerException = null) : TinyBasicException(message, innerException);

public class UnexpectedTokenException(string message) : ParsingException(message);

public class InvalidLabelException(int value) : ParsingException($"Label should be greater than 0 and less than 32767: {value}");

public class RuntimeException(string message, Exception? innerException = null) : TinyBasicException(message, innerException);

public class DivisionByZeroException(short value) : RuntimeException($"Tried to divide {value} by zero");

public class UninitializedVariableException(char address) : RuntimeException($"Tried to use an uninitialized variable: {address}");