using System.Diagnostics;
using System.IO.Pipes;
using TinyBasicCSharp.Tokenization;

namespace TinyBasicCSharp.Environment;

/// <summary>
/// Class for sending information to the console process of debugger.
/// </summary>
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

    /// <summary>
    /// Checks if the connection exists, and if it doesn't - restarts it
    /// </summary>
    public async Task EnsureConnected()
    {
        if (_process is { HasExited: false } && _stream is { IsConnected: true })
        { return; }

        if (_process == null || _process.HasExited)
        {
            if (_writer != null)
            { await _writer.DisposeAsync(); }
            _process?.Kill();
            _process?.Dispose();
            _process = null;
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
    
    /// <summary>
    /// Sends a signal to the console process to update an existing line with the new statement
    /// </summary>
    /// <param name="statement">New statement to replace old one</param>
    /// <param name="index">Old statement index</param>
    public async Task UpdateLine(Statement statement, short index)
    {
        var stringToSend = $"u:{index}:{statement}";
        await _writer!.WriteLineAsync(stringToSend);
    }

    /// <summary>
    /// Sends a signal to the console process to append a new statement
    /// </summary>
    /// <param name="statement">New statement</param>
    /// <param name="label">New statement label</param>
    public async Task AddLine(Statement statement, short label)
    {
        var stringToSend = $"a:{label}:{statement}";
        await _writer!.WriteLineAsync(stringToSend);
    }

    private async Task AddLine(string line, short label) => await _writer!.WriteLineAsync($"a:{label}:{line}");
    
    /// <summary>
    /// Sends a signal to the console process to remove a line
    /// </summary>
    /// <param name="index">Index of the statement to be removed</param>
    public async Task RemoveLine(short index)
    {
        var stringToSend = $"r:{index}";
        await _writer!.WriteLineAsync(stringToSend);
    }

    /// <summary>
    /// Sends a signal to the console process to clear the console and print current state.
    /// </summary>
    public async Task Print()
    { await _writer!.WriteLineAsync("print"); }

    /// <summary>
    /// Sends a signal to the console process to place/remove a breakpoint
    /// </summary>
    /// <param name="breakpoint">Line number</param>
    public async Task UpdateBreakpoint(short breakpoint)
    {
        var stringToSend = $"b:{breakpoint}";
        await _writer!.WriteLineAsync(stringToSend);
    }

    /// <summary>
    /// Sends a signal to the console process to update the cursor
    /// </summary>
    /// <param name="currentLine"></param>
    public async Task UpdateCurrentLine(short currentLine)
    {
        _currentLine = currentLine;
        var stringToSend = $"c:{currentLine}";
        await _writer!.WriteLineAsync(stringToSend);
    }
    
    /// <summary>
    /// Closes the pipe connection and kills the console process.
    /// </summary>
    public async Task Close()
    {
        if (_stream != null)
        {
            await _stream.FlushAsync();
            await _writer!.DisposeAsync();
        }
        
        _process?.Kill();
        _process?.Dispose();
        _process = null;
    }
}