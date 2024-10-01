namespace TinyCompilerForTinyBasic;

public class Lexer
{
    public Lexer(string sourceCode) => _sourceCode = sourceCode;
    private string _sourceCode;
    private int _pointer = 0;
    
    public TBToken[] Tokenize()
    {
        List<TBToken> tokens = [];
        while (_pointer < _sourceCode.Length)
        {
            char currentCharacter = _sourceCode[_pointer];
            if (currentCharacter == ' ')
            { ++_pointer; }
            
            else if (currentCharacter is '"')
            { tokens.Add(ReadString()); }
            
            else if (currentCharacter is '+' or '-' or '/' or '*' or '<' or '>' or '(' or ')' or '=')
            { tokens.Add(ReadOperator()); }
            
            else if (char.IsDigit(currentCharacter))
            { tokens.Add(ReadNumber()); }

            else if (char.IsLetter(currentCharacter))
            {
                string value = ReadVariableOrKeyword();
                if (value is "PRINT" or "IF" or "GOTO" or "INPUT" or "LET" or "GOSUB" or "RETURN" or "CLEAR" or "LIST" or "RUN" or "END" or "THEN")
                { tokens.Add(new TBToken() { Type = TBTokenType.Keyword, Value = value }); }
                else
                { tokens.Add(new TBToken() { Type = TBTokenType.Variable, Value = value }); }
            }

            else
            { throw new Exception($"Unexpected character '{currentCharacter}'"); }
        }
        
        return tokens.ToArray();
    }

    private TBToken ReadString()
    {
        ++_pointer;
        int pointerCopy = _pointer;
        while(Peek() != '"' && _pointer < _sourceCode.Length)
        { ++_pointer; }
        
        if (Peek() is '\0')
        { throw new Exception("Failed to find closing quotation mark."); }
        
        string value = _sourceCode.Substring(pointerCopy, _pointer - pointerCopy + 1);
        _pointer += 2;
        return new TBToken(){ Type = TBTokenType.String, Value = value };
    }
    
    private string ReadVariableOrKeyword()
    {
        int pointerCopy = _pointer;
        while(char.IsLetter(Peek()))
        { ++_pointer; }
        
        string value = _sourceCode.Substring(pointerCopy, _pointer - pointerCopy + 1);
        ++_pointer;
        return value;
    }
    
    private TBToken ReadOperator()
    {
        TBToken token;
        switch (_sourceCode[_pointer])
        {
            case '<':
                if (Peek() is '>')
                {
                    token = new TBToken() { Type = TBTokenType.Operator, Value = "<>" };
                    _pointer += 2;
                }
                else
                { 
                    token = new TBToken() { Type = TBTokenType.Operator, Value = "<" };
                    ++_pointer;
                }
                return token;
            case '>':
                if (Peek() is '<')
                {
                    token = new TBToken() { Type = TBTokenType.Operator, Value = "><" };
                    _pointer += 2;
                }
                else
                { 
                    token = new TBToken() { Type = TBTokenType.Operator, Value = ">" };
                    ++_pointer;
                }
                return token;
            case '(':
            case ')':
                token = new TBToken() { Type = TBTokenType.Parenthesis, Value = _sourceCode[_pointer].ToString() };
                ++_pointer;
                return token;
            default:
                token = new TBToken() { Type = TBTokenType.Operator, Value = _sourceCode.Substring(_pointer, 1) };
                ++_pointer;
                return token;
        }
    }
    
    private TBToken ReadNumber()
    {
        int pointerCopy = _pointer;
        char nextCharacter = Peek();
        while (char.IsDigit(nextCharacter))
        {
            ++_pointer;
            nextCharacter = Peek();
        }

        TBToken token = new() { Type = TBTokenType.Number, Value = _sourceCode.Substring(pointerCopy, _pointer - pointerCopy + 1) };
        ++_pointer;
        return token;
    }
    
    private char Peek() => (_pointer + 1) < _sourceCode.Length ? _sourceCode[_pointer + 1] : '\0';
}