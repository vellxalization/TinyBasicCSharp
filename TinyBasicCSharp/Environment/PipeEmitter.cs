using System.Diagnostics;
using System.IO.Pipes;
using TinyCompilerForTinyBasic.Tokenization;

namespace TinyCompilerForTinyBasic.Environment;

public class PipeEmitter
{
    private Process? _process;
    private NamedPipeServerStream? _stream;
    private StreamWriter? _writer;

    // copy of the state to allow autoreloading 
    private HashSet<short> _breakpoints;
    private short _currentLine;
    private SortedList<short, (Statement statement, bool isLabeled)> _program;

    public PipeEmitter(HashSet<short> breakpoints, SortedList<short, (Statement statement, bool isLabeled)> program)
    {
        _program = program;
        _breakpoints = breakpoints;
    }

    public async Task EnsureConnected()
    {
        Console.WriteLine("Ensuring");
        if (_process is { HasExited: false } && _stream is { IsConnected: true })
        { return; }

        if (_process == null || _process.HasExited)
        {
            if (_writer != null)
            { await _writer.DisposeAsync(); }
            _process?.Kill();
            _process?.Dispose();
            StartProcess();
        }

        if (_stream == null || !_stream.IsConnected)
        { await CreatePipe(); }
        await SendInitialState();
    }

    private async Task CreatePipe()
    {
        _stream = new NamedPipeServerStream("tbDebuggerPipe", PipeDirection.Out);
        await _stream.WaitForConnectionAsync();
        _writer = new StreamWriter(_stream) { AutoFlush = true };
    }

    private void StartProcess()
    {
        _process = new Process();
        _process.StartInfo.FileName = "DebuggingConsole";
        _process.StartInfo.UseShellExecute = true;
        if (!_process.Start())
        { throw new Exception("Failed to start debugging console."); }
    }
    
    private async Task SendInitialState()
    {
        foreach (var (label, statement) in _program)
        {
            var stringToWrite = statement.isLabeled ? statement.statement.ToString() : $"({label}) {statement.statement}";
            await AddLine(stringToWrite, label);
        }
        await Print();
    
        foreach (var breakpoint in _breakpoints)
        { await UpdateBreakpoint(breakpoint); }
        await UpdateCurrentLine(_currentLine);
    }
    
    public async Task UpdateLine(Statement statement, short index)
    {
        var stringToSend = $"u:{index}:{statement}";
        await _writer!.WriteLineAsync(stringToSend);
    }

    public async Task AddLine(Statement statement, short label)
    {
        var stringToSend = $"a:{label}:{statement}";
        await _writer!.WriteLineAsync(stringToSend);
    }

    private async Task AddLine(string line, short label) => await _writer!.WriteLineAsync($"a:{label}:{line}");
    
    public async Task RemoveLine(short index)
    {
        var stringToSend = $"r:{index}";
        await _writer!.WriteLineAsync(stringToSend);
    }

    public async Task Print()
    { await _writer!.WriteLineAsync("print"); }

    public async Task UpdateBreakpoint(short breakpoint)
    {
        var stringToSend = $"b:{breakpoint}";
        await _writer!.WriteLineAsync(stringToSend);
    }

    public async Task UpdateCurrentLine(short currentLine)
    {
        _currentLine = currentLine;
        var stringToSend = $"c:{currentLine}";
        await _writer!.WriteLineAsync(stringToSend);
    }
    
    public async Task Close()
    {
        if (_stream != null)
        {
            await _stream.FlushAsync();
            await _writer!.DisposeAsync();
        }
        
        _process?.Kill();
        _process?.Dispose();
    }
}