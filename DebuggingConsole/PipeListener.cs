using System.IO.Pipes;

namespace DebuggingConsole;

public class PipeListener
{
    private NamedPipeClientStream _pipe = new(".", "tbDebuggerPipe", PipeDirection.In);
    private StreamReader? _reader;
    
    public async Task Listen()
    {
        if (!_pipe.IsConnected)
        { await _pipe.ConnectAsync(); }
        
        _reader = new StreamReader(_pipe);
        string? line;
        while ((line = await _reader.ReadLineAsync()) != null)
        {
            if (line == "clear")
            {
                Console.Clear();
                continue;
            }

            var prefix = line[..3];
            if (prefix is "[B]") // breakpoint
            {
                Console.BackgroundColor = ConsoleColor.DarkRed;
                Console.Write(line[3..]);
                Console.BackgroundColor = ConsoleColor.Black;
                Console.WriteLine("");
            }
            else if (prefix is "[C]") // current line
            {
                Console.BackgroundColor = ConsoleColor.DarkYellow;
                Console.Write($"—>{line[3..]}");
                Console.BackgroundColor = ConsoleColor.Black;
                Console.WriteLine("");
            }
            else
            { Console.WriteLine(line); }
        }
    }
}