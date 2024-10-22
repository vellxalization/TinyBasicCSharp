using System.Text;
using TinyCompilerForTinyBasic.Parsing;

namespace TinyCompilerForTinyBasic.Environment;

public class TinyBasicEnvironment
{
    public ConsoleCancelEventHandler CancelHandler { get; }
    
    private SortedList<int, (TinyBasicToken[] line, bool isLabeled)> _program = new();
    private EnvironmentMemory _memory = new();
    private ExpressionEvaluator _evaluator;
    private bool _isRunning = false;
    private int _lineKeyIndex = 0;
    
    private Queue<short> _inputQueue = new();
    private Queue<int> _returnQueue = new();
    
    public TinyBasicEnvironment()
    {
        CancelHandler = (_, args) =>
        {
            args.Cancel = true;
            if (!_isRunning)
            { return; }
            
            Console.WriteLine("Execution terminated");
            _isRunning = false;
        };
        _evaluator = new ExpressionEvaluator(_memory);
    }
    
    public void ExecuteFile(string sourceCode)
    {
        var lexer = new Lexer(sourceCode);
        var parser = new LineParser(lexer.Tokenize());
    
        int lastPointer = 0;
        while (parser.CanReadLine())
        {
            TinyBasicToken[] line = parser.ParseLine();
            if (line[0].Type is TBTokenType.Number)
            { AddToProgram(line, int.Parse(line[0].ToString()), true); }
            else
            { AddToProgram(line, ++lastPointer, false); }
        }
        ExecuteProgram();
    }
    
    public void ExecuteDirectly(string line)
    {
        var lexer = new Lexer(line);
        LineParser parser = new(lexer.Tokenize());
        TinyBasicToken[] parsedLine = parser.ParseLine();
        
        if (parsedLine[0].Type is TBTokenType.Number)
        { AddToProgram(parsedLine, int.Parse(parsedLine[0].ToString()), true); }
        else
        { ExecuteLine(parsedLine); }
    }

    private void AddToProgram(TinyBasicToken[] line, int position, bool isLabeled)
    {
        if (!isLabeled && (line.Length < 2 || line[1].Type is TBTokenType.NewLine))
        {
            _program.Remove(position);
            return;
        }
        
        if (!_program.TryAdd(position, (line, isLabeled)))
        { _program[position] = (line, isLabeled); }
    }
    
    private void ExecuteLine(TinyBasicToken[] line)
    {
        int commandIndex = line[0].Type is TBTokenType.Number ? 1 : 0; // skip line number
        string command = line[commandIndex].ToString();
        switch (command)
        {
            case "LET":
            {
                char address = char.Parse(line[commandIndex + 1].ToString());
                var expression = (ExpressionTinyBasicToken)line[commandIndex + 3];
                short value = _evaluator.EvaluateExpression(expression.Components);
                _memory.WriteVariable(value, address);
                break;
            }
            case "PRINT":
            {
                string text = ExpressionListToString(line);
                Console.WriteLine(text);
                break;
            }
            case "INPUT":
            {
                List<char> addresses = new();
                foreach (TinyBasicToken token in line)
                {
                    if ((char.TryParse(token.ToString(), out char address)) && (address is > 'A' and < 'Z'))
                    { addresses.Add(address); }
                }
                LoadAddressesWithValues(addresses);
                break;
            }
            case "GOTO":
            {
                GoToLabel((commandIndex + 1), line, false);
                break;
            }
            case "GOSUB":
            {
                GoToLabel((commandIndex + 1), line, true);
                break;
            }
            case "RETURN":
            {
                if (!_returnQueue.TryDequeue(out int lineNumber))
                { throw new RuntimeException("Tried to return without invoking a subroutine"); }
                
                _lineKeyIndex = lineNumber;
                break;
            }
            case "END":
            {
                _isRunning = false;
                break;
            }
            case "LIST":
            {
                PrintProgram();
                break;
            }
            case "CLEAR":
            {
                _inputQueue.Clear();
                _returnQueue.Clear();
                _lineKeyIndex = 0;
                _program.Clear();
                break;
            }
            case "IF":
            {
                TinyBasicToken op = line[commandIndex + 2];
                
                var expression = (ExpressionTinyBasicToken)line[commandIndex + 1];
                short value1 = _evaluator.EvaluateExpression(expression.Components);
                expression = (ExpressionTinyBasicToken)line[commandIndex + 3];
                short value2 = _evaluator.EvaluateExpression(expression.Components);
                
                if (CheckCondition(value1, value2, op))
                { ExecuteLine(line[(commandIndex + 5)..]); }
                
                break;
            }
            case "RUN":
            {
                ExecuteProgram();
                break;
            }
        }
    }

    private void ExecuteProgram()
    {
        _lineKeyIndex = 0;
        IList<int> keys = _program.Keys;

        _isRunning = true;
        while (_isRunning && _lineKeyIndex < keys.Count)
        {
            int key = keys[_lineKeyIndex];
            TinyBasicToken[] line = _program[key].line;
            ExecuteLine(line);
            ++_lineKeyIndex;
        }
        _isRunning = false;
    }
    
    private void PrintProgram()
    {
        var builder = new StringBuilder();

        foreach (var (line, _) in _program.Values)
        {
            foreach (TinyBasicToken token in line)
            {
                builder.Append(token);
                builder.Append(' ');
            }
            Console.WriteLine(builder.ToString());
            builder.Clear();
        }
    }

    private void GoToLabel(int start, TinyBasicToken[] line, bool isSubroutine)
    {
        var expression = (ExpressionTinyBasicToken)line[start];
        short label = _evaluator.EvaluateExpression(expression.Components);
        if ((!_program.TryGetValue(label, out var instruction)) || (!instruction.isLabeled))
        { throw new Exception($"Label {label} does not exist"); }

        if (isSubroutine)
        { _returnQueue.Enqueue(_lineKeyIndex); }
        
        _lineKeyIndex = (_program.IndexOfKey(label) - 1); // decrement to compensate increment in ExecuteProgram()
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

            List<ExpressionTinyBasicToken> input = RequestInput();
            foreach (ExpressionTinyBasicToken expression in input)
            {
                value = _evaluator.EvaluateExpression(expression.Components);
                _inputQueue.Enqueue(value);
            }
        }
    }

    private List<ExpressionTinyBasicToken> RequestInput()
    {
        Console.WriteLine('?');
        string? input = Console.ReadLine();
        if (input is null)
        { return []; }
        
        List<ExpressionTinyBasicToken> expressions = new();
        var lexer = new Lexer(input);
        TinyBasicToken[] tokens = lexer.Tokenize();
        int pointer = 0;
        while (pointer < tokens.Length)
        {
            ExpressionTinyBasicToken expression = ParsingUtils.SelectExpressionFromLine(tokens, ref pointer);
            try
            { ParsingUtils.ParseExpression(expression); }
            catch (Exception ex)
            { throw new Exception($"Error parsing expression: {ex.Message}"); } // TODO: placeholder
            
            expressions.Add(expression);
            pointer += 2;
        }
        return expressions;
    }
    
    private string ExpressionListToString(TinyBasicToken[] line)
    {
        var builder = new StringBuilder();
        int pointer = line[0].Type is TBTokenType.Number ? 2 : 1;
        while (pointer < line.Length)
        {
            TinyBasicToken token = line[pointer];
            switch (token.Type)
            {
                case TBTokenType.Comma:
                {
                    ++pointer;
                    continue;
                }
                case TBTokenType.QuotedString:
                {
                    builder.Append(token);
                    break;
                }
                default:
                {
                    builder.Append(_evaluator.EvaluateExpression(((ExpressionTinyBasicToken)token).Components));
                    break;
                }
            }
            ++pointer;
        }
        return builder.ToString();
    }
    
    private bool CheckCondition(short value1, short value2, TinyBasicToken op)
    {
        return op.ToString() switch
        {
            "<" => value1 < value2,
            "<=" => value1 <= value2,
            ">" => value1 > value2,
            ">=" => value1 >= value2,
            "=" => value1 == value2,
            _ => throw new Exception("Unknown operator") // will be caught by parser, exists just to close default case
        };
    }
}