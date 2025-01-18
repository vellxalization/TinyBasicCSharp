using TinyCompilerForTinyBasic.Parsing;
using TinyCompilerForTinyBasic.Tokenization;

namespace TinyCompilerForTinyBasic.Environment;

public class FileEnvironment : TinyBasicEnvironment
{
    public void LoadFile(string sourceCode)
    {
        Clear();
        var tokens = TokenizeInput(sourceCode);
        if (tokens.Length == 0)
        { return; }
        
        var parser = new LineParser(tokens);
        CurrentLineIndex = 1; // we use it here and only here, not as an index, but as a label 
        while (parser.CanReadLine())
        {
            if (!parser.ParseLine(out Statement statement, out string? error))
            {
                int lineNumber = statement.Label ?? CurrentLineIndex;
                Console.WriteLine($"Line {lineNumber}: Syntax error:\n >{error}");
                return;
            }

            if (statement is { Label: null, StatementType: StatementType.Newline })
            { continue; }
            UpdateProgram(statement);
        }
    }

    public void ExecuteLoadedFile() => ExecuteProgram();

    protected override void UpdateProgram(Statement statement)
    {
        var label = statement.Label;
        if (label != null)
        {
            base.UpdateProgram(statement);
            CurrentLineIndex = (short)(label.Value + 1);
            return;
        }

        if (statement.StatementType == StatementType.Newline)
        { throw new ArgumentException("Statement must have a label or shouldn't be a new line"); }
        ++CurrentLineIndex;
        Program[CurrentLineIndex] = (statement, false);
    }
}