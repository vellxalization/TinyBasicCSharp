// using System.Text;
// using TinyCompilerForTinyBasic.Parsing;
//
// namespace TinyCompilerForTinyBasic;
//
// public class TBEnvironment
// {
//     private List<TinyBasicToken[]> _program = [];
//     private short?[] _memory = new short?[26];
//     private int _linePointer = 0;
//     private Dictionary<int, int> _labelsMap = new();
//     
//     private Parser _parser;
//     
//     private Queue<short> _inputQueue = new();
//     private Queue<int> _returnQueue = new();
//     
//     public TBEnvironment(string sourceCode)
//     {
//         var lexer = new Lexer(sourceCode);
//         TinyBasicToken[] tokens = lexer.Tokenize();
//         
//         _parser = new Parser(tokens);
//     }
//
//     public void Execute()
//     {
//         CreateStack();
//         while (_linePointer < _program.Count)
//         {
//             ExecuteLine(_program[_linePointer++]);
//         }
//     }
//
//     private void ExecuteLine(TinyBasicToken[] line)
//     {
//         int commandIndex = line[0].Type is TBTokenType.Number ? 1 : 0; // skip line number
//         switch (line[commandIndex].Value)
//         {
//             case "LET":
//             {
//                 char address = char.Parse(line[(commandIndex + 1)].Value!);
//                 int start = (commandIndex + 3);
//                 short value = EvaluateExpression(line, ref start);
//                 WriteDataToMemory(address, value);
//                 break;
//             }
//             case "PRINT":
//             {
//                 string text = ExpressionListToString(line, (commandIndex + 1));
//                 Print(text);
//                 break;
//             }
//             case "INPUT":
//             {
//                 List<char> addresses = new();
//                 for (int i = (commandIndex + 1); i < line.Length; ++i)
//                 {
//                     TinyBasicToken tinyBasicToken = line[i];
//                     if (tinyBasicToken.Type is TBTokenType.Separator)
//                     { continue; }
//                     else
//                     { addresses.Add(char.Parse(tinyBasicToken.Value!)); }
//                 };
//                 
//                 LoadAddressesWithValues(addresses);
//                 break;
//             }
//             case "GOTO":
//             {
//                 int start = (commandIndex + 1);
//                 short label = EvaluateExpression(line, ref start);
//                 if (!_labelsMap.TryGetValue(label, out int lineNumber))
//                 { throw new Exception("Label does not exist"); }
//                 else
//                 { _linePointer = lineNumber; }
//                 break;
//             }
//             case "GOSUB":
//             {
//                 int start = (commandIndex + 1);
//                 short label = EvaluateExpression(line, ref start);
//                 if (!_labelsMap.TryGetValue(label, out int lineNumber))
//                 { throw new Exception("Label does not exist"); }
//                 else
//                 {
//                     _returnQueue.Enqueue(_linePointer);
//                     _linePointer = lineNumber;
//                 }
//                 break;
//             }
//             case "RETURN":
//             {
//                 if (!_returnQueue.TryDequeue(out int lineNumber))
//                 { throw new Exception("Tried to return without invoking a subroutine"); }
//                 
//                 _linePointer = lineNumber;
//                 break;
//             }
//             case "END":
//             {
//                 Environment.Exit(0);
//                 break;
//             }
//             case "LIST":
//             {
//                 var builder = new StringBuilder();
//                 
//                 foreach (TinyBasicToken[] programLine in _program)
//                 {
//                     foreach (TinyBasicToken token in programLine)
//                     {
//                         builder.Append(token.Value);
//                         builder.Append(' ');
//                     }
//                     Console.WriteLine(builder.ToString());
//                     builder.Clear();
//                 }
//                 break;
//             }
//             case "CLEAR":
//             {
//                 _inputQueue.Clear();
//                 _returnQueue.Clear();
//                 _program = [];
//                 _memory = new short?[26];
//                 _linePointer = 0;
//                 _labelsMap.Clear();
//                 _parser = new Parser([]);
//                 break;
//             }
//             case "IF":
//             {
//                 int start = (commandIndex + 1);
//                 short value1 = EvaluateExpression(line, ref start);
//                 TinyBasicToken op = line[start++];
//                 short value2 = EvaluateExpression(line, ref start);
//                 ++start;
//                 if (CheckCondition(value1, value2, op))
//                 { ExecuteLine(line[start..]); }
//                 
//                 break;
//             }
//         }
//     }
//
//     private bool CheckCondition(short value1, short value2, TinyBasicToken op)
//     {
//         return op.Value switch
//         {
//             "<" => value1 < value2,
//             "<=" => value1 <= value2,
//             ">" => value1 > value2,
//             ">=" => value1 >= value2,
//             "=" => value1 == value2,
//             _ => throw new Exception("Unknown operator") // will be caught by parser, exists just to close default case
//         };
//     }
//
//     private void LoadAddressesWithValues(List<char> addresses)
//     {
//         int pointer = 0;
//         while (pointer < addresses.Count)
//         {
//             if (_inputQueue.TryDequeue(out short value))
//             {
//                 // _memory[addresses[pointer++]] = value;
//                 WriteDataToMemory(addresses[pointer++], value);
//                 continue;
//             }
//
//             TinyBasicToken[] input = RequestInput();
//             List<TinyBasicToken[]> separated = SeparateInput(input);
//             foreach (TinyBasicToken[] expression in separated)
//             {
//                 if (!ExpressionParser.ParseExpression(expression))
//                 { throw new Exception("Invalid expression from input"); }
//
//                 int start = 0;
//                 _inputQueue.Enqueue(EvaluateExpression(expression, ref start));
//             }
//         }
//     }
//     
//     private List<TinyBasicToken[]> SeparateInput(TinyBasicToken[] input)
//     {
//         List<TinyBasicToken[]> separated = new();
//         int pointerCopy = 0;
//         for (int i = 0; i < input.Length; ++i)
//         {
//             TinyBasicToken tinyBasicToken = input[i];
//             if (tinyBasicToken.Type is TBTokenType.Separator)
//             {
//                 separated.Add(input[pointerCopy..i]);
//                 pointerCopy = i + 1;
//             }
//         }
//         if (pointerCopy < input.Length)
//         { separated.Add(input[pointerCopy..]); }
//         
//         return separated;
//     }
//     
//     private TinyBasicToken[] RequestInput()
//     {
//         Console.WriteLine('?');
//         string? input = Console.ReadLine();
//         if (string.IsNullOrEmpty(input))
//         { return []; }
//         
//         var lexer = new Lexer(input);
//         return lexer.Tokenize();
//     }
//     
//     private void Print(string text) => Console.WriteLine(text);
//
//     private string ExpressionListToString(TinyBasicToken[] line, int startIndex)
//     {
//         var builder = new StringBuilder();
//         while (startIndex < line.Length)
//         {
//             TinyBasicToken tinyBasicToken = line[startIndex];
//             if (tinyBasicToken.Type is TBTokenType.QuotedString)
//             { builder.Append(tinyBasicToken.Value); }
//             else if (tinyBasicToken.Type is not TBTokenType.Separator)
//             { builder.Append(EvaluateExpression(line, ref startIndex)); }
//             ++startIndex;
//         }
//         
//         return builder.ToString();
//     }
//     
//     private void WriteDataToMemory(char address, short value) => _memory[address - 'A'] = value;
//
//     private short EvaluateExpression(TinyBasicToken[] line, ref int startIndex)
//     {
//         bool shouldNegate = false;
//         if (line[startIndex].Type is TBTokenType.Operator)
//         {
//             if (line[startIndex].Value is "-")
//             { shouldNegate = true; }
//             
//             ++startIndex;
//         }
//         
//         int value = EvaluateTerm(line, ref startIndex);
//         if (shouldNegate)
//         { value = -value; }
//
//         while (startIndex < line.Length && line[startIndex].Value is "+" or "-")
//         {
//             bool shouldAdd = line[startIndex].Value is "+";
//             ++startIndex;
//             
//             int nextTerm = EvaluateTerm(line, ref startIndex);
//             value = shouldAdd ? value + nextTerm : value - nextTerm;
//         }
//
//         return unchecked((short)value);
//     }
//
//     private int EvaluateTerm(TinyBasicToken[] line, ref int startIndex)
//     {
//         int value = EvaluateFactor(line, ref startIndex);
//         while (startIndex < line.Length && line[startIndex].Value is "*" or "/")
//         {
//             bool shouldMultiply = line[startIndex].Value is "*";
//             ++startIndex;
//             
//             int nextFactor = EvaluateFactor(line, ref startIndex); 
//             if (shouldMultiply)
//             { value *= nextFactor; }
//             else
//             {
//                 if (nextFactor is 0)
//                 { throw new Exception("Division by zero"); }
//                 
//                 value /= nextFactor;
//             }
//         }
//
//         return value;
//     }
//     
//     private int EvaluateFactor(TinyBasicToken[] line, ref int startIndex)
//     {
//         int? value;
//         
//         TinyBasicToken tinyBasicToken = line[startIndex];
//         if (tinyBasicToken.Type is TBTokenType.Number)
//         { value = int.Parse(tinyBasicToken.Value!); }
//         else if (tinyBasicToken.Value is "(")
//         {
//             ++startIndex;
//             value = EvaluateExpression(line, ref startIndex);
//         }
//         else
//         {
//             char address = char.Parse(tinyBasicToken.Value!);
//             value = _memory[address - 'A'];
//             if (value is null)
//             { throw new Exception("Uninitialized variable"); }
//         }
//         
//         ++startIndex;
//         return value.Value;
//     }
//     
//     private void CreateStack()
//     {
//         while (_parser.GetCurrentToken() is not null)
//         {
//             if (!_parser.ParseLine(out TinyBasicToken[] line))
//             { throw new Exception("Error parsing line"); }
//
//             if (line[0].Type is TBTokenType.NewLine)
//             { continue; }
//             
//             if (line[0].Type is TBTokenType.Number)
//             {
//                 int label = int.Parse(line[0].Value!);
//                 if (label is < 0 or > 32767)
//                 { throw new Exception("Bad line number"); }
//                 
//                 if (line.Length > 1)
//                 { _labelsMap.Add(label, _program.Count); }
//                 else
//                 { _labelsMap.Remove(label); }
//             }
//             _program.Add(line);
//         }
//     }
// }