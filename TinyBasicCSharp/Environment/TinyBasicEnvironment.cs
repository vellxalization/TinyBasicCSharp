using System.Text;
using TinyCompilerForTinyBasic.Parsing;
using TinyCompilerForTinyBasic.Tokenization;

namespace TinyCompilerForTinyBasic.Environment;

public class TinyBasicEnvironment
{
    public ConsoleCancelEventHandler CancelHandler { get; }
    
    private SortedList<short, (Statement statement, bool isLabeled)> _program = new();
    private EnvironmentMemory _memory = new();
    private ExpressionEvaluator _evaluator;
    private bool _isRunning = false;
    private short _lineKeyIndex = 0;
    
    private Stack<short> _returnStack = new();
    private Queue<short> _inputQueue = new();
    
    public TinyBasicEnvironment()
    {
        CancelHandler = (_, args) =>
        {
            if (!_isRunning)
            { return; }
            
            _isRunning = false;
            args.Cancel = true;
            Console.WriteLine("Execution terminated");
        };
        _evaluator = new ExpressionEvaluator(_memory);
    }
    
    public void ExecuteFile(string sourceCode)
    {
        Clear();
        var lexer = new Lexer(sourceCode);
        TinyBasicToken[] tokens;
        
        try
        { tokens = lexer.Tokenize(); }
        catch (TokenizationException ex)
        {
            Console.WriteLine($"Syntax error:\n >{ex.Message}");
            return;
        }
    
        var parser = new LineParser(tokens);
        _lineKeyIndex = 1;
        while (parser.CanReadLine())
        {
            if (!parser.ParseLine(out Statement statement, out string? error))
            {
                int lineNumber = statement.Label ?? _lineKeyIndex;
                Console.WriteLine($"Line {lineNumber}: Syntax error:\n >{error}");
                return;
            }

            if (statement.Arguments.Length is 0 || statement.StatementType is StatementType.Newline)
            { continue; }
            
            UpdateProgram(statement);
        }

        ExecuteProgram();
    }
    
    public void ExecuteDirectly(string line)
    {
        var lexer = new Lexer(line);
        TinyBasicToken[] tokens;
        try
        { tokens = lexer.Tokenize(); }
        catch (TokenizationException ex)
        {
            Console.WriteLine($"Syntax error:\n >{ex.Message}");
            return;
        }
        
        if (tokens.Length == 0 || tokens[0].Type is TokenType.NewLine)
        { return; }
        
        LineParser parser = new(tokens);
        if (!parser.ParseLine(out var statement, out string? error))
        {
            Console.WriteLine($"Syntax error:\n >{error}");
            return;
        }
        
        if (statement.Label is not null)
        {
            UpdateProgram(statement); 
            return;
        }
        try
        { ExecuteLine(statement); }
        catch (RuntimeException ex)
        { Console.WriteLine($"Runtime error: {ex.Message}"); }
    }
    
    private void UpdateProgram(Statement statement)
    {
        bool isLabeled = statement.Label is not null;
        if (!isLabeled)
        {
            if (!_program.TryAdd(_lineKeyIndex, (statement, false)))
            { _program[_lineKeyIndex] = (statement, false); }
            
            ++_lineKeyIndex;
            return;
        }

        short label = statement.Label!.Value;
        if (statement.StatementType is StatementType.Newline)
        { _program.Remove(statement.Label!.Value); }
        else
        {
            if (!_program.TryAdd(label, (statement, true)))
            { _program[label] = (statement, true); }
        }
        
        if (label >= _lineKeyIndex)
        { _lineKeyIndex = (short)(label + 1); }
    }
    
    private void ExecuteLine(Statement statement)
    {
        var arguments = statement.Arguments;
        switch (statement.StatementType)
        {
            case StatementType.Let:
            {
                ExecuteLet(arguments);
                return;
            }
            case StatementType.Print:
            {
                string text = ExpressionListToString(arguments);
                Console.WriteLine(text);
                return;
            }
            case StatementType.Input:
            {
                ExecuteInput(arguments);
                return;
            }
            case StatementType.Goto:
            {
                GoToLabel(statement, false);
                return;
            }
            case StatementType.Gosub:
            {
                GoToLabel(statement, true);
                return;
            }
            case StatementType.Return:
            {
                ExecuteReturn();
                return;
            }
            case StatementType.End:
            {
                _isRunning = false;
                return;
            }
            case StatementType.List:
            {
                PrintProgram();
                return;
            }
            case StatementType.Clear:
            {
                Clear();
                return;
            }
            case StatementType.If:
            {
                ExecuteIf(arguments);
                return;
            }
            case StatementType.Run:
            {
                ExecuteProgram();
                return;
            }
            case StatementType.Rem:
            { return; }
            default:
            { throw new RuntimeException($"Tried to execute unknown command"); }
        }
    }

    private void ExecuteReturn()
    {
        if (!_returnStack.TryPop(out short lineNumber))
        { throw new RuntimeException("Tried to return without invoking a subroutine"); }

        _lineKeyIndex = lineNumber;
    }

    private void ExecuteInput(TinyBasicToken[] arguments)
    {
        List<char> addresses = new();
        foreach (TinyBasicToken token in arguments)
        {
            switch (token.Type)
            {
                case TokenType.Comma:
                { continue; }
                case TokenType.String:
                {
                    char address = char.Parse(token.ToString());
                    addresses.Add(address);
                    continue;
                }
                default:
                { throw new RuntimeException($"Got unexpected token in variable list: {token}"); }
            }
        }
        LoadAddressesWithValues(addresses);
    }

    private void ExecuteLet(TinyBasicToken[] arguments)
    {
        char address = char.Parse(arguments[0].ToString());
        var expression = (ExpressionToken)arguments[^1];
        short value = _evaluator.EvaluateExpression(expression);
        _memory.WriteVariable(value, address);
    }

    private void ExecuteIf(TinyBasicToken[] arguments)
    {
        var expression = (ExpressionToken)arguments[0];
        var value1 = _evaluator.EvaluateExpression(expression);
        expression = (ExpressionToken)arguments[2];
        var value2 = _evaluator.EvaluateExpression(expression);
        
        var op = arguments[1];
        if (!CheckCondition(value1, value2, op))
        { return; }

        var nextStatement = (Statement)arguments[^1];
        ExecuteLine(nextStatement);
    }
    
    private void Clear()
    {
        _inputQueue.Clear();
        _returnStack.Clear();
        _lineKeyIndex = 0;
        _program.Clear();
    }

    private void ExecuteProgram()
    {
        _lineKeyIndex = 0;

        _isRunning = true;
        while (_isRunning && _lineKeyIndex < _program.Count)
        {
            Statement statement = _program.GetValueAtIndex(_lineKeyIndex).statement;
            try
            { ExecuteLine(statement); }
            catch (RuntimeException ex)
            {
                Console.WriteLine($"Line {_program.GetKeyAtIndex(_lineKeyIndex)}: Runtime error: {ex.Message}");
                return;
            }
            ++_lineKeyIndex;
        }

        if (_isRunning)
        { Console.WriteLine("Runtime error: Run out of lines. Possibly missed the END or RETURN keyword?"); }
        _isRunning = false;
    }
    
    private void PrintProgram()
    {
        foreach (var (statement, _) in _program.Values)
        { Console.WriteLine(statement); }
    }

    private void GoToLabel(Statement statement, bool isSubroutine)
    {
        var expression = (ExpressionToken)statement.Arguments[0];
        short label = _evaluator.EvaluateExpression(expression);
        if ((!_program.TryGetValue(label, out var instruction)) || (!instruction.isLabeled))
        { throw new RuntimeException($"Label {label} does not exist"); }

        if (isSubroutine)
        { _returnStack.Push(_lineKeyIndex); }
        
        _lineKeyIndex = (short)(_program.IndexOfKey(label) - 1); // decrement to compensate increment in ExecuteProgram()
    }
    
    private void LoadAddressesWithValues(List<char> addresses)
    {
        int pointer = 0;
        while (pointer < addresses.Count)
        {
            if (_inputQueue.TryDequeue(out short value))
            {
                _memory.WriteVariable(value, addresses[pointer]);
                ++pointer;
                continue;
            }

            List<ExpressionToken> input = RequestInput();
            foreach (ExpressionToken expression in input)
            {
                try
                { value = _evaluator.EvaluateExpression(expression); }
                catch (RuntimeException ex)
                {
                    Console.WriteLine($"Input queue: Runtime error: {ex.Message}");
                    continue;
                }
                _inputQueue.Enqueue(value);
            }
        }
    }

    private List<ExpressionToken> RequestInput()
    {
        Console.WriteLine('?');
        string? input = Console.ReadLine();
        if (string.IsNullOrEmpty(input))
        { return []; }
        
        List<ExpressionToken> expressions = new();
        var lexer = new Lexer(input);
        TinyBasicToken[] tokens;
        try
        { tokens = lexer.Tokenize(); }
        catch (TokenizationException ex)
        {
            Console.WriteLine($"Syntax error:\n >{ex.Message}");
            return [];
        }
        
        int pointer = 0;
        while (pointer < tokens.Length)
        {
            var expressionSpan = ExpressionParser.SelectExpressionFromLine(tokens, pointer);
            try
            {
                var expression = ExpressionParser.ParseExpression(expressionSpan);
                expressions.Add(expression);
            }
            catch (ParsingException ex)
            { throw new ParsingException($"Error parsing expression:\n >{ex.Message}"); } 
            
            pointer += expressionSpan.Length;
            pointer += 2;
        }
        return expressions;
    }
    
    private string ExpressionListToString(TinyBasicToken[] exprList)
    {
        var builder = new StringBuilder();
        foreach (var token in exprList)
        {
            switch (token.Type)
            {
                case TokenType.Comma:
                { continue; }
                case TokenType.QuotedString:
                {
                    builder.Append(token);
                    break;
                }
                case TokenType.Expression:
                {
                    builder.Append(_evaluator.EvaluateExpression((ExpressionToken)token));
                    break;
                }
                default:
                { throw new RuntimeException($"Got unexpected token type in expression list: {token}"); }
            }
        }
      
        return builder.ToString();
    }
    
    private bool CheckCondition(short value1, short value2, TinyBasicToken op)
    {
        return op.Type switch
        {
            TokenType.OperatorGreaterThan => value1 > value2,
            TokenType.OperatorGreaterThanOrEqual => value1 >= value2,
            TokenType.OperatorLessThan => value1 < value2,
            TokenType.OperatorLessThanOrEqual => value1 <= value2,
            TokenType.OperatorEquals => value1 == value2,
            TokenType.OperatorNotEqual => value1 != value2,
            _ => throw new Exception("Unknown operator") // will be caught by parser, exists just to close default case
        };
    }
}