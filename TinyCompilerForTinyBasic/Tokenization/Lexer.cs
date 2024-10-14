using System.Text;

namespace TinyCompilerForTinyBasic;

public class Lexer
{
    public Lexer(string sourceCode) => _sourceCode = sourceCode;
    private string _sourceCode;
    private int _pointer = 0;

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
            { throw new TokenizationException($"Unexpected character '{current}'"); }
        }

        return tokens.ToArray();
    }

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
        { throw new TokenizationException($"Failed to find closing quotation mark for: {_sourceCode.Substring(pointerCopy, _pointer - pointerCopy)}"); }
        
        return new ValueTinyBasicToken(TBTokenType.QuotedString, _sourceCode.Substring(pointerCopy, _pointer - pointerCopy));
    }

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
            { throw new TokenizationException("Unexpected operator"); } 
        }
    }
}