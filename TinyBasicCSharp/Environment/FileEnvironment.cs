using TinyBasicCSharp.Parsing;
using TinyBasicCSharp.Tokenization;

namespace TinyBasicCSharp.Environment;

public class FileEnvironment : TinyBasicEnvironment
{
    public bool LoadFile(string sourceCode)
    {
        Clear();
        var tokens = TokenizeInput(sourceCode);
        if (tokens.Length == 0)
        { return false; }

        var lines = Parser.SplitByNewline(tokens);
        CurrentLineIndex = 1; // we use it here and only here, not as an index, but as a label 
        foreach (var line in lines)
        {
            if (line.Length == 0)
            { continue; }
            
            try
            {
                var parsedLine = Parser.ParseStatement(line);
                if (parsedLine is { Label: null, Type: StatementType.Newline })
                { continue; }

                UpdateProgram(parsedLine);
            }
            catch(ParsingException ex)
            {
                Console.WriteLine($"Error parsing line {(line[0] is NumberToken number 
                    ? number.Value
                    : CurrentLineIndex)}:");
                ex.PrintException();
                return false;
            }
        }

        return true;
    }

    public void ExecuteLoadedFile() => ExecuteProgram();

    protected override void UpdateProgram(Statement statement)
    {
        var label = statement.Label;
        if (label != null)
        {
            base.UpdateProgram(statement);
            var newValue = (short)(label.Value + 1);
            if (newValue >= CurrentLineIndex)
            { CurrentLineIndex = newValue; }
            return;
        }

        if (statement.Type == StatementType.Newline)
        { throw new ArgumentException("Statement must have a label or shouldn't be a new line"); }
        
        Program[CurrentLineIndex] = (statement, false);
        ++CurrentLineIndex;
    }
}