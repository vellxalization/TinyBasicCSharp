namespace TinyCompilerForTinyBasic;

public class Lexer
{
    public Lexer(string sourceCode) => _sourceCode = sourceCode;
    private string _sourceCode;
    private int _pointer = 0;

    public TBToken[] Tokenize()
    {
        var tokens = new List<TBToken>();
        while (_pointer < _sourceCode.Length)
        {
            char current = _sourceCode[_pointer];
            
            if (char.IsWhiteSpace(current))
            { ++_pointer; }
            else if (current is '"')
            { tokens.Add(ReadQuotedString()); }
            else if (current is '(' or ')' or '+' or '-' or '*' or '/' or '<' or '>' or '=')
            { tokens.Add(ReadOperatorOrParenthesis()); }
            else if (char.IsDigit(current))
            { tokens.Add(ReadNumber()); }
            else if (char.IsLetter(current))
            { tokens.Add(ReadString()); }
            else
            { throw new Exception($"Unexpected character '{current}'"); }
        }
        return tokens.ToArray();
    }

    private TBToken ReadNumber()
    {
        int pointerCopy = _pointer;
        char next = Peek();
        while (char.IsDigit(next) || char.IsWhiteSpace(next))
        {
            ++_pointer;
            next = Peek();
        } 
        
        ++_pointer;
        string value = _sourceCode.Substring(pointerCopy, _pointer - pointerCopy).Trim();
        return new TBToken(TBTokenType.Number, value);
    }

    private TBToken ReadQuotedString()
    {
        int pointerCopy = _pointer;
        char next = Peek();
        while (next != '"' && next != '\0')
        {
            ++_pointer;
            next = Peek();
        }
        
        if (next == '\0')
        { throw new Exception("Failed to find matching quotation mark"); }

        _pointer += 2;
        string value = _sourceCode.Substring(pointerCopy, _pointer - pointerCopy);
        return new TBToken(TBTokenType.QuotedString, value);
    }

    private TBToken ReadString()
    {
        int pointerCopy = _pointer;
        char next = Peek();
        while (char.IsLetter(next))
        {
            ++_pointer;
            next = Peek();
        } 

        ++_pointer;
        string value = _sourceCode.Substring(pointerCopy, _pointer - pointerCopy);
        return new TBToken(TBTokenType.String, value);
    }

    private TBToken ReadOperatorOrParenthesis()
    {
        char current = _sourceCode[_pointer];
        TBToken token;
        switch (current)
        {
            case '+':
            case '-':
            case '*':
            case '/':
            case '=':
                token = new TBToken(TBTokenType.Operator, current.ToString());
                ++_pointer;
                break;
            case '(':
            case ')':
                token = new TBToken(TBTokenType.Parenthesis, current.ToString());
                ++_pointer;
                break;
            case '<':
            case '>':
                char next = Peek();
                if (next == '=' || ((current == '<' && next == '>') || (current == '>' && next == '<')))
                {
                    token = new TBToken(TBTokenType.Operator, string.Concat(current, next));
                    _pointer += 2;
                }
                else
                {
                    token = new TBToken(TBTokenType.Operator, current.ToString());
                    ++_pointer;
                };
                break;
            default:
                throw new Exception($"Unrecognized operator '{current}'");
        }

        return token;
    }
    
    private char Peek() => ((_pointer + 1) < _sourceCode.Length) ? _sourceCode[_pointer + 1] : '\0';
}