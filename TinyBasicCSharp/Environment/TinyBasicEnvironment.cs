using System.Text;
using TinyBasicCSharp.Parsing;
using TinyBasicCSharp.Tokenization;

namespace TinyBasicCSharp.Environment;

public class TinyBasicEnvironment
{
    public ConsoleCancelEventHandler CancelHandler { get; }

    protected SortedList<short, (Statement statement, bool isLabeled)> Program = new();
    protected readonly EnvironmentMemory Memory = new();
    private readonly ExpressionEvaluator _evaluator;
    protected bool IsRunning = false;
    protected short CurrentLineIndex = 0;
    
    protected readonly Stack<short> ReturnStack = new();
    private readonly Queue<short> _inputQueue = new();

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
    
    /// <summary>
    /// Creates a debugging environment from this instance of environment
    /// </summary>
    /// <returns></returns>
    public DebugEnvironment CreateDebugEnvironment() => new(Program); 
    
    /// <summary>
    /// Method for executing single isolated line of TinyBasic code. If it's not labeled - it will be executed immediately.
    /// Otherwise - put in the stack to be executed with RunProgram() method
    /// </summary>
    /// <param name="line">Line of TinyBasic code</param>
    public void ExecuteDirectly(string line)
    {
        var tokens = TokenizeInput(line);
        if (tokens.Length == 0 || tokens[0] is ServiceToken { Type: ServiceType.Newline })
        { return; }

        Statement? statement;
        try
        { statement = Parser.ParseStatement(tokens); }
        catch (ParsingException ex)
        {
            Console.WriteLine("Syntax error:");
            ex.PrintException();
            return;
        }
        ExecuteDirectly(statement);
    }

    /// <summary>
    /// Stops execution of the current program
    /// </summary>
    public void TerminateExecution()
    {
        CurrentLineIndex = short.MinValue;
        IsRunning = false;
    }
    
    protected void ExecuteDirectly(Statement statement)
    {
        if (statement.Label is not null)
        { UpdateProgram(statement); }
        else
        {
            try
            { ExecuteStatement(statement); }
            catch (RuntimeException ex)
            {
                Console.WriteLine("Runtime error:");
                ex.PrintException();
            }
        }
    }
    
    protected IToken[] TokenizeInput(string input)
    {
        try
        { return Lexer.Tokenize(input); }
        catch (TokenizationException ex)
        {
            ex.PrintException();
            return [];
        }
    }

    protected virtual void UpdateProgram(Statement statement)
    {
        var label = statement.Label;
        if (label == null)
        { throw new ArgumentException("Can't add statement without a label"); }

        if (statement.Type is StatementType.Newline)
        { Program.Remove(label.Value); }
        else
        {
            if (!Program.TryAdd(label.Value, (statement, true)))
            { Program[label.Value] = (statement, true); }
        }
    }
    
    protected void ExecuteStatement(Statement statement)
    {
        var arguments = statement.Arguments;
        switch (statement.Type)
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
                GoToLabel(arguments, false);
                return;
            }
            case StatementType.Gosub:
            {
                GoToLabel(arguments, true);
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
    
    private void ExecuteReturn()
    {
        if (!ReturnStack.TryPop(out short lineNumber))
        { throw new RuntimeException("Tried to return without invoking a subroutine"); }

        CurrentLineIndex = lineNumber;
    }
    
    private void ExecuteLet(IToken[] arguments)
    {
        char address = char.Parse(arguments[0].ToString()!);
        var expression = (ExpressionToken)arguments[^1];
        short value = _evaluator.EvaluateExpression(expression);
        Memory.WriteVariable(value, address);
    }

    private void ExecuteIf(IToken[] arguments)
    {
        var expression = (ExpressionToken)arguments[0];
        var value1 = _evaluator.EvaluateExpression(expression);
        expression = (ExpressionToken)arguments[2];
        var value2 = _evaluator.EvaluateExpression(expression);
        
        var op = (OperatorToken)arguments[1];
        if (!CheckCondition(value1, value2, op))
        { return; }

        var nextStatement = (Statement)arguments[^1];
        ExecuteStatement(nextStatement);
    }
    
    private bool CheckCondition(short value1, short value2, OperatorToken op)
    {
        return op.Type switch
        {
            OperatorType.GreaterThan => value1 > value2,
            OperatorType.GreaterThanOrEqual => value1 >= value2,
            OperatorType.LessThan => value1 < value2,
            OperatorType.LessThanOrEqual => value1 <= value2,
            OperatorType.Equals => value1 == value2,
            OperatorType.NotEqual => value1 != value2,
            _ => throw new Exception("Unknown operator")
        };
    }

    protected void Clear()
    {
        TerminateExecution();
        _inputQueue.Clear();
        ReturnStack.Clear();
        Program.Clear();
    }

    protected void ExecuteProgram()
    {
        CurrentLineIndex = 0;
        IsRunning = true;
        while (IsRunning && CurrentLineIndex < Program.Count)
        {
            Statement statement = Program.GetValueAtIndex(CurrentLineIndex).statement;
            try
            { ExecuteStatement(statement); }
            catch (RuntimeException ex)
            {
                var lineNumber = Program.GetKeyAtIndex(CurrentLineIndex);
                Console.WriteLine($"Line {lineNumber}: Runtime error:");
                ex.PrintException();
                return;
            }
            ++CurrentLineIndex;
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

    private void GoToLabel(IToken[] arguments, bool isSubroutine)
    {
        var labelToken = (NumberToken)arguments[0];
        var label = (short)labelToken.Value;
        if (!Program.TryGetValue(label, out var instruction) || !instruction.isLabeled)
        { throw new RuntimeException($"Label {label} does not exist"); }

        if (isSubroutine)
        { ReturnStack.Push(CurrentLineIndex); }
        
        CurrentLineIndex = (short)(Program.IndexOfKey(label) - 1); // decrement to compensate increment in ExecuteProgram()
    }
    
    private void ExecuteInput(IToken[] arguments)
    {
        List<char> addresses = new();
        foreach (var token in arguments)
        {
            switch (token)
            {
                case ServiceToken { Type: ServiceType.Comma }:
                { continue; }
                case WordToken word:
                {
                    char address = char.Parse(word.Value);
                    addresses.Add(address);
                    continue;
                }
                default:
                { throw new RuntimeException($"Got unexpected token in variable list: {token}"); }
            }
        }
        if (addresses.Count == 0)
        { return; }
        LoadAddressesWithValues(addresses);
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

            var input = RequestValuesForInput();
            foreach (var expression in input)
            {
                try
                { value = _evaluator.EvaluateExpression(expression); }
                catch (RuntimeException ex)
                {
                    Console.WriteLine("Input queue: Runtime error:");
                    ex.PrintException();
                    continue;
                }
                _inputQueue.Enqueue(value);
            }
        }
    }

    private ExpressionToken[] RequestValuesForInput()
    {
        Console.WriteLine('?');
        string? input = Console.ReadLine();
        if (string.IsNullOrEmpty(input))
        { return []; }

        var tokens = TokenizeInput(input);
        if (tokens.Length == 0) 
        { return []; }

        try
        { return ParseInput(tokens); }
        catch (ParsingException ex)
        {
            Console.WriteLine("Syntax error:");
            ex.PrintException();
            return [];
        }
    }

    private ExpressionToken[] ParseInput(IToken[] input)
    {
        var pointer = 0;
        var expressions = new List<ExpressionToken>();
        while (pointer < input.Length)
        {
            var token = input[pointer];
            if (token is ServiceToken { Type: ServiceType.Comma })
            {
                ++pointer;
                if (pointer >= input.Length)
                { throw new ParsingException("Expected next expression after the comma"); }
                
                continue;
            }
            var expressionSpan = ExpressionParser.SelectExpressionFromLine(input, pointer);
            if (expressionSpan.Length == 0)
            { throw new ParsingException($"Expected an expression, got {token}"); }
            
            var expression = ExpressionParser.ParseExpression(expressionSpan);
            expressions.Add(expression);
            
            pointer += expressionSpan.Length;
        }
        return expressions.ToArray();
    }
    
    private string ExpressionListToString(IToken[] exprList)
    {
        var builder = new StringBuilder();
        for (int i = 0; i < exprList.Length; i += 2)
        {
            var token = exprList[i];
            switch (token)
            {
                case QuotedStringToken qString:
                {
                    var unquotedString = qString.Value.Trim('"');
                    builder.Append(unquotedString);
                    break;
                }
                case ExpressionToken expression:
                {
                    builder.Append(_evaluator.EvaluateExpression(expression));
                    break;
                }
                default:
                { throw new RuntimeException($"Got unexpected token type in expression list: {token}"); }
            }
        }
        return builder.ToString();
    }
}