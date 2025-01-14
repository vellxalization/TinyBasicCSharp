using System.IO.Pipes;

namespace DebuggingConsole;

public class PipeListener
{
    private NamedPipeClientStream _pipe = new(".", "tbDebuggerPipe", PipeDirection.In);
    private StreamReader? _reader;
    
    // state
    private readonly HashSet<short> _breakPoints = [];
    private readonly SortedList<short, string> _program = new();
    private short _currentLine = 0;
    
    public async Task Listen()
    {
        if (!_pipe.IsConnected)
        { await _pipe.ConnectAsync(); }
        
        _reader = new StreamReader(_pipe);
        string? line;
        while ((line = await _reader.ReadLineAsync()) != null)
        {
            line = line.TrimEnd();
            if (line.Length < 2)
            {
                Console.WriteLine($"Got bad command: {line}, skipping. . .");
                continue;
            }
            if (line == "print")
            {
                Console.Clear();
                for (int i = 0; i < _program.Count; ++i)
                {
                    if (i == _currentLine)
                    { PrintColoredLine(_program.GetValueAtIndex(i), ConsoleColor.DarkYellow); }
                    else if (_breakPoints.Contains(_program.GetKeyAtIndex(i)))
                    { PrintColoredLine(_program.GetValueAtIndex(i), ConsoleColor.DarkRed); }
                    else
                    { Console.WriteLine(_program.GetValueAtIndex(i)); }
                }
                continue;
            }
            
            var prefix = line[..2];
            switch (prefix)
            {
                case "b:": // update breakpoint
                {
                    var label = short.Parse(line[2..]);
                    var index = _program.IndexOfKey(label);
                    if (_breakPoints.Add(label))
                    { ChangeColorOfLine(index, ConsoleColor.DarkRed); }
                    else
                    {
                        ChangeColorOfLine(index, ConsoleColor.Black);
                        _breakPoints.Remove(label);
                    }
                    break;
                }
                case "c:": // update current line pointer
                {
                    var index = short.Parse(line[2..]);
                    ChangeColorOfLine(_currentLine, _breakPoints.Contains(_program.GetKeyAtIndex(_currentLine)) 
                        ? ConsoleColor.DarkRed : ConsoleColor.Black);
                    ChangeColorOfLine(index, ConsoleColor.DarkYellow);
                    _currentLine = index;
                    break;
                }
                case "r:": // remove line
                {
                    var index = short.Parse(line[2..]);
                    _program.RemoveAt(index);
                    break;
                }
                case "a:": // add line
                {
                    var separator = line.IndexOf(':', 3);
                    var label = short.Parse(line[2..separator]);
                    var statement = line[(separator + 1)..];
                    _program.Add(label, statement);
                    break;
                }
                case "u:": // update line
                {
                    var separator = line.IndexOf(':', 3);
                    var index = short.Parse(line[2..separator]);
                    var statement = line[(separator + 1)..];
                    _program.SetValueAtIndex(index, statement);
                    break;
                }
                default:
                {
                    Console.WriteLine($"Got non-prefix command: {line}, skipping");
                    break;
                }
            }
        }
    }

    private void ChangeColorOfLine(int consoleRow, ConsoleColor color)
    {
        var originalPos = Console.GetCursorPosition();
        Console.SetCursorPosition(0, consoleRow);
        PrintColoredLine(_program.GetValueAtIndex(consoleRow), color);
        Console.SetCursorPosition(originalPos.Left, originalPos.Top);
    }

    private static void PrintColoredLine(string statement, ConsoleColor bgColor)
    {
        Console.BackgroundColor = bgColor;
        Console.WriteLine(statement);
        Console.BackgroundColor = ConsoleColor.Black;
    }
}