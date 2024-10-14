using System.Text;
using TinyCompilerForTinyBasic.Environment;
using TinyCompilerForTinyBasic.Parsing;

namespace TinyCompilerForTinyBasic;

public class TBEnvironment
{
    private List<TinyBasicToken[]> _program = [];
    private EnvironmentMemory _memory = new();
    private Dictionary<int, int> _labelsMap = new();
    private ExpressionEvaluator _evaluator;
    private int _linePointer = 0;
    
    private Queue<short> _inputQueue = new();
    private Queue<int> _returnQueue = new();

    public TBEnvironment()
    {
        _evaluator = new ExpressionEvaluator(_memory);
    }
    
    public void ExecuteLoadedCode()
    {
        while (_linePointer < _program.Count)
        {
            ExecuteLine(_program[_linePointer]);
            ++_linePointer;
        }
    }

    public void LoadCode(string sourceCode)
    {
        Lexer lexer = new Lexer(sourceCode);
        AddToStack(lexer.Tokenize());
    }

    private void AddToStack(TinyBasicToken[] tokens)
    {
        LineParser parser = new LineParser(tokens);
        while (parser.CanReadLine())
        {
            TinyBasicToken[] line = parser.ParseLine();
            if (line[0].Type is TBTokenType.NewLine)
            { continue; }
            
            if (line[0].Type is TBTokenType.Number)
            { UpdateLabel(int.Parse(line[0].ToString()), line); }
            _program.Add(line);
        }
    }

    // TODO: double check this later
    private void UpdateLabel(int label, TinyBasicToken[] line)
    {
        if ((line.Length < 2) || (line[1].Type is TBTokenType.NewLine))
        { _labelsMap.Remove(label); }
        else
        { _labelsMap.Add(label, _program.Count); }
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
                
                _linePointer = lineNumber;
                break;
            }
            case "END":
            {
                System.Environment.Exit(0);
                break;
            }
            case "LIST":
            {
                var builder = new StringBuilder();
                
                foreach (TinyBasicToken[] programLine in _program)
                {
                    foreach (TinyBasicToken token in programLine)
                    {
                        builder.Append(token.ToString());
                        builder.Append(' ');
                    }
                    Console.WriteLine(builder.ToString());
                    builder.Clear();
                }
                break;
            }
            case "CLEAR":
            {
                _inputQueue.Clear();
                _returnQueue.Clear();
                _program = [];
                _memory = new EnvironmentMemory();
                _evaluator = new(_memory);
                _linePointer = 0;
                _labelsMap.Clear();
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
        }
    }

    private void GoToLabel(int start, TinyBasicToken[] line, bool isSubroutine)
    {
        var expression = (ExpressionTinyBasicToken)line[start];
        short label = _evaluator.EvaluateExpression(expression.Components);
        if (!_labelsMap.TryGetValue(label, out int lineNumber))
        { throw new Exception("Label does not exist"); }

        if (isSubroutine)
        { _returnQueue.Enqueue(_linePointer); }
        _linePointer = (lineNumber - 1);
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
            }
            else
            {
                List<ExpressionTinyBasicToken> input = RequestInput();
                foreach (ExpressionTinyBasicToken expression in input)
                {
                    value = _evaluator.EvaluateExpression(expression.Components);
                    _inputQueue.Enqueue(value);
                }
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