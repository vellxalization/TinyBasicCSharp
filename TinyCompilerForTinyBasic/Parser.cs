namespace TinyCompilerForTinyBasic;

public class Parser
{
    private TBToken[] _tokens;
    private int _pointer;
    private short?[] _variables = [];
    private HashSet<short> _declaredLabels = [];
    private HashSet<short> _expectedLabels = [];
    
    public Parser(TBToken[] tokens) => _tokens = tokens;

    public bool Parse()
    {
        _pointer = 0;
        _variables = new short?[26];
        _declaredLabels.Clear();
        _expectedLabels.Clear();
        
        while (_pointer < _tokens.Length)
        {
            if (!ParseLine())
            { return false; }
            ++_pointer;
            
            if (Peek() is null) // end of tokens
            { break; }
            ++_pointer;
        }
        
        return ValidateLabels();
    }

    private bool ValidateLabels()
    {
        _expectedLabels.ExceptWith(_declaredLabels);
        return _expectedLabels.Count == 0;
    }
    
    // line ::= number statement CR | statement CR
    private bool ParseLine()
    {
        TBToken token = GetCurrentToken()!;
        if (token.Type is TBTokenType.Number)
        {
            if (Peek() is null)
            { return false; } // expected a statement after number

            int value = int.Parse(token.Value!);
            if (value is < 1 or > 32767)
            { return false; } // bad line number
            
            _declaredLabels.Add((short)value);
            ++_pointer;
        }

        if (!ParseStatement())
        { return false; }

        TBToken? next = Peek();
        if (next is null || next.Value is "\n")
        { return true; }

        return false; // unexpected token
    }
    
    // statement ::= PRINT expr-list
    //     IF expression relop expression THEN statement
    //     GOTO expression
    //     INPUT var-list
    //     LET var = expression
    //     GOSUB expression
    //     RETURN
    //     CLEAR
    //     LIST
    //     RUN
    //     END
    private bool ParseStatement()
    {
        TBToken token = GetCurrentToken()!;
        if (token.Type is not TBTokenType.String)
        { return false; } // expected a keyword

        switch (token.Value)
        {
            case "PRINT":
                return ParsePrint();
            case "IF":
                return ParseIf();
            case "GOSUB":
            case "GOTO":
                return ParseGotoGosub();
            case "INPUT":
                return ParseInput();
            case "LET":
                return ParseLet();
            case "RETURN":
            case "CLEAR":
            case "LIST":
            case "RUN":
            case "END":
                return true;
            default:
                return false; // unexpected keyword
        }
    }

    private bool ParsePrint()
    {
        if (Peek() is null)
        { return false; } // expected an expression list after PRINT keyword

        ++_pointer;
        return ParseExpressionList();
    }
    
    private bool ParseGotoGosub()
    {
        if (Peek() is null)
        { return false; } // expected an expression after GOTO/GOSUB keyword
        
        ++_pointer;
        if (!ParseExpression(out short? evaluated))
        { return false; }

        _expectedLabels.Add(evaluated!.Value);
        return true;
    }
    
    private bool ParseInput()
    {
        if (Peek() is null)
        { return false; } // expected a var-list after INPUT keyword
        
        ++_pointer;
        return ParseVarList();
    }
    
    private bool ParseLet()
    {
        if ((!char.TryParse(Peek()?.Value, out char address) || (address is < 'A' or > 'Z')))
        { return false; } // expected a valid variable name after LET keyword
        ++_pointer;
        
        if (Peek()?.Value is not "=")
        { return false; } // expected an equal sign after variable name
        ++_pointer;
        
        if (Peek() is null)
        { return false; } // expected an expression after = operator
        ++_pointer;
        
        if (!ParseExpression(out short? evaluated))
        { return false; }

        SetVariableValue(address, evaluated!.Value);
        return true;
    }
    
    private bool ParseIf()
    {
        if (Peek() is null)
        { return false; } // expected an expression after IF keyword
        ++_pointer;
        
        if (!ParseExpression(out _))
        { return false; }
        
        if (Peek()?.Type is not TBTokenType.Operator)
        { return false; } // expected an operator after expression
        ++_pointer;
        
        if (Peek() is null)
        { return false; } // expected an expression after operator
        ++_pointer;
        
        if (!ParseExpression(out _))
        { return false; }
        
        if (Peek()?.Value is not "THEN")
        { return false; } // expected THEN keyword after second expression
        ++_pointer;
        
        if (Peek() is null)
        { return false; } // expected a statement after THEN keyword
        ++_pointer;
        
        if (!ParseStatement())
        { return false; }
        
        return true;
    }
    
    // expr-list ::= (string|expression) (, (string|expression) )*
    private bool ParseExpressionList()
    {
        TBToken token = GetCurrentToken()!;
        if (token.Type is not TBTokenType.QuotedString)
        {
            if (!ParseExpression(out _))
            { return false; }
        }

        while (Peek()?.Type is TBTokenType.Separator)
        {
            ++_pointer;
            if (Peek() is null)
            { return false; } // expected next string or expression after comma
            
            ++_pointer;
            token = GetCurrentToken()!;
            if (token.Type is not TBTokenType.QuotedString)
            {
                if (!ParseExpression(out _))
                { return false; }
            }
        }

        return true;
    }
    
    // var-list ::= var (, var)*
    private bool ParseVarList()
    {
        TBToken? token = GetCurrentToken();
        if ((!char.TryParse(token?.Value, out char address)) || (address is < 'A' or > 'Z'))
        { return false; } // expected a valid variable name

        while (Peek()?.Type is TBTokenType.Separator)
        {
            ++_pointer;
            if (Peek() is null)
            { return false; } // expected next variable after separator
            
            ++_pointer;
            token = GetCurrentToken();
            if ((!char.TryParse(token?.Value, out address)) || (address is < 'A' or > 'Z'))
            { return false; } 
        }

        return true;
    }

    // expression ::= (+|-|ε) term ((+|-) term)*
    private bool ParseExpression(out short? evaluated)
    {
        evaluated = null;
        bool shouldNegate = false;
        TBToken? token = GetCurrentToken();
        if (token?.Value is ("+" or "-"))
        {
            if (Peek() is null)
            { return false; } // expected a term after + or -
            if (token.Value is "-")
            { shouldNegate = true; }
            
            ++_pointer; 
        }
        
        if (!ParseTerm(out evaluated))
        { return false; }
        if (shouldNegate)
        { evaluated = (short)-evaluated!.Value; }

        TBToken? next = Peek();
        while (next?.Value is ("+" or "-"))
        {
            bool shouldAdd = next.Value is "+";
            ++_pointer; 
            if (Peek() is null)
            { return false; } // expected a term
            
            ++_pointer; 
            if (!ParseTerm(out short? nextEvaluated))
            { return false; }

            evaluated = (short)(shouldAdd ? unchecked(evaluated + nextEvaluated) : unchecked(evaluated - nextEvaluated))!;
            next = Peek();
        }

        return true; // parsed last term
    }

    // term ::= factor ((*|/) factor)*
    private bool ParseTerm(out short? evaluated)
    {
        evaluated = null;
        
        if (!ParseFactor(out evaluated))
        { return false; }

        TBToken? next = Peek();
        while (next?.Value is ("*" or "/"))
        {
            bool shouldMultiply = next.Value is "*";
            ++_pointer; 
            if (Peek() is null)
            { return false; } // expected next factor
            
            ++_pointer; 
            if (!ParseFactor(out short? nextEvaluated))
            { return false; }

            if (!shouldMultiply && nextEvaluated is 0)
            {
                evaluated = null;
                return false; // tried to divide by zero
            } 
            
            evaluated = (short)(shouldMultiply ? unchecked(evaluated * nextEvaluated) : unchecked(evaluated / nextEvaluated))!;
            next = Peek();
        }
        
        return true; // parsed last factor
    }
    
    // factor ::= var | number | (expression)
    private bool ParseFactor(out short? evaluated)
    {
        evaluated = null;
        
        TBToken token = GetCurrentToken()!;
        if (token.Type is TBTokenType.Number)
        {
            evaluated = (short)(int.Parse(token.Value!) % 65536);
            return true;
        }
        if (token.Value is "(")
        {
            if (Peek() is null)
            { return false; } // expected an expression
            ++_pointer;
            
            if (!ParseExpression(out evaluated))
            { return false; }
            
            if (Peek()?.Value is not ")") // expected a closing parenthesis
            { return false; }

            ++_pointer;
            return true;
        }
        if ((char.TryParse(token.Value, out char address)) && (address is >= 'A' and <= 'Z') && (GetVariableValue(address, out short? value)))
        {
            evaluated = value;
            return true;
        }

        return false; // failed to parse factor
    }

    private void SetVariableValue(char address, short value) => _variables[address - 'A'] = value;
    private bool GetVariableValue(char address, out short? value)
    {
        value = _variables[address - 'A'];
        return true;
    }
    
    private TBToken? GetCurrentToken() => (_pointer < _tokens.Length) ? _tokens[_pointer] : null;
    private TBToken? Peek() => ((_pointer + 1) < _tokens.Length) ? _tokens[_pointer + 1] : null;
}