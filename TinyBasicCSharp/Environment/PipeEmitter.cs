using System.Diagnostics;
using System.IO.Pipes;

namespace TinyCompilerForTinyBasic.Environment;

public class PipeEmitter
{
    private NamedPipeServerStream _stream = new("tbDebuggerPipe", PipeDirection.Out);
    private StreamWriter _writer;
    private Process? _process;
    
    public async Task WriteLine(string line, bool isBreak, bool isCurrentLine)
    {
        if (!_stream.IsConnected)
        {
            if (!(await Init()))
            { throw new Exception("bruh"); }
        }
        await _writer.WriteLineAsync(isBreak ? $"[B]{line}" 
            : isCurrentLine ? $"[C]:{line}" 
            : line);
    }

    public async Task Close()
    {
        await _stream.FlushAsync();
        _stream.Close();
        _process?.Kill();
    }

    private async Task<bool> Init()
    {
        if (_process?.HasExited ?? false)
        {
            _process.Close();
            _process.Dispose();
        }
        _process = new Process();
        _process.StartInfo.FileName = "DebuggingConsole";
        _process.StartInfo.UseShellExecute = true;
        if (!_process.Start())
        { throw new Exception("Failed to start debugging console."); }
        
        try
        {
            await _stream.WaitForConnectionAsync();
            _writer = new StreamWriter(_stream) { AutoFlush = true };
            Console.WriteLine("connected");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to connect to debugging console: {ex.Message}");
            _process.Kill();
            _stream.Close();
            await _stream.DisposeAsync();
            await _writer.DisposeAsync();
            return false;
        }
    }
}