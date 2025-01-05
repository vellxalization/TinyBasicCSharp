using System.Text;
using TinyCompilerForTinyBasic.Parsing;
using TinyCompilerForTinyBasic.Tokenization;

namespace TinyCompilerForTinyBasic.Environment;

public class TinyBasicEnvironment
{
    public ConsoleCancelEventHandler CancelHandler { get; }

    protected SortedList<short, (Statement statement, bool isLabeled)> Program = new();
    protected readonly EnvironmentMemory Memory = new();
    private readonly ExpressionEvaluator _evaluator;
    protected bool IsRunning = false;
    protected short LineKeyIndex = 0;
    
    protected readonly Stack<short> ReturnStack = new();
    private readonly Queue<short> _inputQueue = new();

    public DebugEnvironment CreateDebugEnvironment() => new() 
        { Program = Program };

    public TinyBasicEnvironment()
    {
        CancelHandler = (_, args) =>
        {
            if (!IsRunning)
            { return; }
            
            TerminateExecution();
            args.Cancel = true;
            Console.WriteLine("Execution terminated");
        };
        _evaluator = new ExpressionEvaluator(Memory);
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
            AddStatement(statement); 
            return;
        }
        try
        { ExecuteStatement(statement); }
        catch (RuntimeException ex)
        { Console.WriteLine($"Runtime error: {ex.Message}"); }
    }

    protected virtual void AddStatement(Statement statement)
    {
        var label = statement.Label;
        if (label == null)
        { throw new ArgumentException("Can't add statement without a label"); }

        if (statement.StatementType is StatementType.Newline)
        {
            Program.Remove(label.Value);
            return;
        }

        if (!Program.TryAdd(label.Value, (statement, true)))
        { Program[label.Value] = (statement, true); }
    }
    
    protected void ExecuteStatement(Statement statement)
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
                TerminateExecution();
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
            { throw new RuntimeException("Tried to execute unknown command"); }
        }
    }

    protected void TerminateExecution()
    {
        LineKeyIndex = -2;
        IsRunning = false;
    }

    private void ExecuteReturn()
    {
        if (!ReturnStack.TryPop(out short lineNumber))
        { throw new RuntimeException("Tried to return without invoking a subroutine"); }

        LineKeyIndex = lineNumber;
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
        Memory.WriteVariable(value, address);
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
        ExecuteStatement(nextStatement);
    }

    protected void Clear()
    {
        _inputQueue.Clear();
        ReturnStack.Clear();
        LineKeyIndex = -2    ;
        Program.Clear();
    }

    protected void ExecuteProgram()
    {
        LineKeyIndex = 0;
        IsRunning = true;
        while (IsRunning && LineKeyIndex < Program.Count)
        {
            Statement statement = Program.GetValueAtIndex(LineKeyIndex).statement;
            try
            { ExecuteStatement(statement); }
            catch (RuntimeException ex)
            {
                var lineNumber = Program.GetKeyAtIndex(LineKeyIndex);
                Console.WriteLine($"Line {lineNumber}: Runtime error:\n >{ex.Message}");
                return;
            }
            ++LineKeyIndex;
        }

        if (IsRunning)
        { Console.WriteLine("Runtime error: Run out of lines. Possibly missed the END or RETURN keyword?"); }
        TerminateExecution();
    }
    
    private void PrintProgram()
    {
        foreach (var (statement, _) in Program.Values)
        { Console.WriteLine(statement); }
    }

    private void GoToLabel(Statement statement, bool isSubroutine)
    {
        var labelToken = (ValueToken)statement.Arguments[0];
        short label = short.Parse(labelToken.ToString());
        if ((!Program.TryGetValue(label, out var instruction)) || (!instruction.isLabeled))
        { throw new RuntimeException($"Label {label} does not exist"); }

        if (isSubroutine)
        { ReturnStack.Push(LineKeyIndex); }
        
        LineKeyIndex = (short)(Program.IndexOfKey(label) - 1); // decrement to compensate increment in ExecuteProgram()
    }
    
    private void LoadAddressesWithValues(List<char> addresses)
    {
        int pointer = 0;
        while (pointer < addresses.Count)
        {
            if (_inputQueue.TryDequeue(out short value))
            {
                Memory.WriteVariable(value, addresses[pointer]);
                ++pointer;
                continue;
            }

            var input = RequestInput();
            foreach (var expression in input)
            {
                try
                { value = _evaluator.EvaluateExpression(expression); }
                catch (RuntimeException ex)
                {
                    Console.WriteLine($"Input queue: Runtime error:\n >{ex.Message}");
                    continue;
                }
                _inputQueue.Enqueue(value);
            }
        }
    }

    private ExpressionToken[] RequestInput()
    {
        Console.WriteLine('?');
        string? input = Console.ReadLine();
        if (string.IsNullOrEmpty(input))
        { return []; }
        
        var lexer = new Lexer(input);
        TinyBasicToken[] tokens;
        try
        { tokens = lexer.Tokenize(); }
        catch (TokenizationException ex)
        {
            Console.WriteLine($"Syntax error:\n >{ex.Message}");
            return [];
        }

        try
        {
            var inputExpressions = ParseInput(tokens);
            return inputExpressions;
        }
        catch (ParsingException ex)
        {
            Console.WriteLine($"Error parsing INPUT expression:\n >{ex.Message}");
            return [];
        }
    }

    private ExpressionToken[] ParseInput(TinyBasicToken[] input)
    {
        var pointer = 0;
        var expressions = new List<ExpressionToken>();
        while (pointer < input.Length)
        {
            var token = input[pointer];
            if (token.Type is TokenType.Comma)
            {
                if (pointer + 1 >= input.Length)
                { throw new ParsingException("Expected next expression after the comma"); }

                ++pointer;
                continue;
            }
            var expressionSpan = ExpressionParser.SelectExpressionFromLine(input, pointer);
            if (expressionSpan.Length < 1)
            { throw new ParsingException($"Expected an expression, got {token}"); }
            
            var expression = ExpressionParser.ParseExpression(expressionSpan);
            expressions.Add(expression);
            
            pointer += expressionSpan.Length;
        }
        return expressions.ToArray();
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