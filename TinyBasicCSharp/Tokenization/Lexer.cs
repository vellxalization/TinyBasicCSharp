using System.Text;

namespace TinyCompilerForTinyBasic.Tokenization;

/// <summary>
/// Class for tokenizing a string
/// </summary>
public class Lexer
{
    public Lexer(string sourceCode) => _sourceCode = sourceCode;
    private string _sourceCode;
    private int _pointer = 0;

    /// <summary>
    /// Tokenizes input from constructor
    /// </summary>
    /// <returns>An array of TinyBasic tokens</returns>
    /// <exception cref="UnknownCharacterException">Unknown character has occurred during tokenization</exception>
    /// <exception cref="UnmatchedQuotationException">End of input reached without closing quote</exception>
    public TinyBasicToken[] Tokenize()
    {
        var tokens = new List<TinyBasicToken>();
        
        while (_pointer < _sourceCode.Length)
        {
            char current = _sourceCode[_pointer];

            if (current is '\n')
            {
                tokens.Add(new TinyBasicToken(TBTokenType.NewLine));
                ++_pointer;
            }
            else if (char.IsWhiteSpace(current))
            { ++_pointer; }
            else if (current is ',')
            {
                tokens.Add(new TinyBasicToken(TBTokenType.Comma));
                ++_pointer;
            }
            else if (current is '"')
            {
                ++_pointer;
                tokens.Add(ReadQuotedString());
                ++_pointer;
            }
            else if (current is '(' or ')' or '+' or '-' or '*' or '/' or '<' or '>' or '=')
            {
                tokens.Add(ReadOperatorOrParenthesis());
                ++_pointer;
            }
            else if (char.IsDigit(current))
            { tokens.Add(ReadNumber()); }
            else if (char.IsLetter(current))
            { tokens.Add(ReadString()); }
            else
            { throw new UnknownCharacterException($"Unknown character while tokenizing input: '{current}'"); }
        }

        return tokens.ToArray();
    }

    /// <summary>
    /// Used for reading quoted strings.
    /// Pointer should be moved to the next character after opening quotation mark before calling.
    /// Stops on the first quotation mark it encounters
    /// </summary>
    /// <returns>Value token with quoted string</returns>
    /// <exception cref="UnmatchedQuotationException">End of input reached without closing quote</exception>
    private ValueTinyBasicToken ReadQuotedString()
    {
        int pointerCopy = _pointer;

        char currentChar = ' '; // value is not used; instantly overwritten in the loop below
        while (_pointer < _sourceCode.Length)
        {
            currentChar = _sourceCode[_pointer];
            if (currentChar is '"' or '\n' or '\r')
            { break; } 
            // break from the loop if we've found closing quotation mark, new line OR reached the end of source code
            ++_pointer;
        }
        if ((_pointer >= _sourceCode.Length) || (currentChar is '\n' or '\r'))
        { throw new UnmatchedQuotationException($"Failed to find closing quotation mark for: {_sourceCode.Substring(pointerCopy, _pointer - pointerCopy)}"); }
        
        return new ValueTinyBasicToken(TBTokenType.QuotedString, _sourceCode.Substring(pointerCopy, _pointer - pointerCopy));
    }
    
    /// <summary>
    /// Used to read numbers.
    /// Stops at the first letter or newline
    /// </summary>
    /// <returns>Value token with number</returns>
    private ValueTinyBasicToken ReadNumber()
    {
        var builder = new StringBuilder();
        
        while (_pointer < _sourceCode.Length)
        {
            char currentChar = _sourceCode[_pointer];
            if (currentChar is ' ') 
            {
                // numbers with spaces between digits are allowed, so we skip any spaces
                ++_pointer;
                continue;
            }

            if (char.IsDigit(currentChar))
            {
                builder.Append(currentChar);
                ++_pointer;
            }
            else
            { break; }
            // break from the loop if we've encountered a non-digit OR reached the end of source code
        }
        
        return new ValueTinyBasicToken(TBTokenType.Number, builder.ToString());
    }

    /// <summary>
    /// Used to read strings such as variables and keywords.
    /// Stops at the first non-letter character
    /// </summary>
    /// <returns>Value token with string</returns>
    private ValueTinyBasicToken ReadString()
    {
        int pointerCopy = _pointer;
        
        while (_pointer < _sourceCode.Length)
        {
            char currentChar = _sourceCode[_pointer];
            if (!char.IsLetter(currentChar))
            { break; } 
            // break from the loop if character is not letter OR reached the end of source code
            ++_pointer;
        }
        
        return new ValueTinyBasicToken(TBTokenType.String, _sourceCode.Substring(pointerCopy, _pointer - pointerCopy));
    }
    
    /// <summary>
    /// Reads an operator or parenthesis
    /// </summary>
    /// <returns>Token with type according to parsed operator or parenthesis</returns>
    private TinyBasicToken ReadOperatorOrParenthesis()
    {
        switch (_sourceCode[_pointer])
        {
            case '(':
            { return new TinyBasicToken(TBTokenType.ParenthesisOpen); }
            case ')':
            { return new TinyBasicToken(TBTokenType.ParenthesisClose); }
            case '+':
            { return new TinyBasicToken(TBTokenType.OperatorPlus); }
            case '-':
            { return new TinyBasicToken(TBTokenType.OperatorMinus); }
            case '*':
            { return new TinyBasicToken(TBTokenType.OperatorMultiplication); }
            case '/':
            { return new TinyBasicToken(TBTokenType.OperatorDivision); }
            case '=':
            { return new TinyBasicToken(TBTokenType.OperatorEquals); }
            case '>':
            {
                if (((_pointer + 1) >= _sourceCode.Length))
                { return new TinyBasicToken(TBTokenType.OperatorGreaterThan); }

                char nextChar = _sourceCode[_pointer + 1];
                switch (nextChar)
                {
                    case '<':
                        ++_pointer;
                        return new TinyBasicToken(TBTokenType.OperatorNotEqual);
                    case '=':
                        ++_pointer;
                        return new TinyBasicToken(TBTokenType.OperatorGreaterThanOrEqual);
                    default:
                        return new TinyBasicToken(TBTokenType.OperatorGreaterThan);
                }
            }
            case '<':
            {
                if (((_pointer + 1) >= _sourceCode.Length))
                { return new TinyBasicToken(TBTokenType.OperatorLessThan); }

                char nextChar = _sourceCode[_pointer + 1];
                switch (nextChar)
                {
                    case '>':
                        ++_pointer;
                        return new TinyBasicToken(TBTokenType.OperatorNotEqual);
                    case '=':
                        ++_pointer;
                        return new TinyBasicToken(TBTokenType.OperatorLessThanOrEqual);
                    default:
                        return new TinyBasicToken(TBTokenType.OperatorLessThan);
                }
            }
            default: // shouldn't ever get here; exists just to close default switch statement
            { throw new UnknownCharacterException($"Unexpected operator: {_sourceCode[_pointer]}"); } 
        }
    }
}