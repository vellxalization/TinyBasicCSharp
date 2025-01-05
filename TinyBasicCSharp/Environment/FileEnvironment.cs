using TinyCompilerForTinyBasic.Parsing;
using TinyCompilerForTinyBasic.Tokenization;

namespace TinyCompilerForTinyBasic.Environment;

public class FileEnvironment : TinyBasicEnvironment
{
    public void LoadFile(string sourceCode)
    {
        Clear();
        var lexer = new Lexer(sourceCode);
        TinyBasicToken[] tokens;

        try
        { tokens = lexer.Tokenize(); }
        catch (TokenizationException ex)
        {
            Console.WriteLine($"Syntax error:\n >{ex.Message}");
            return;
        }

        var parser = new LineParser(tokens);
        LineKeyIndex = 1;
        while (parser.CanReadLine())
        {
            if (!parser.ParseLine(out Statement statement, out string? error))
            {
                int lineNumber = statement.Label ?? LineKeyIndex;
                Console.WriteLine($"Line {lineNumber}: Syntax error:\n >{error}");
                return;
            }

            if (statement is { Label: null, StatementType: StatementType.Newline })
            { continue; }
            AddStatement(statement);
        }
    }

    public void ExecuteLoadedFile() => ExecuteProgram();

    protected override void AddStatement(Statement statement)
    {
        var label = statement.Label;
        if (label != null)
        {
            base.AddStatement(statement);
            return;
        }

        if (statement.StatementType == StatementType.Newline)
        { throw new ArgumentException("Statement must have a label or shouldn't be a new line"); }
        var lastLabel = Program.Count == 0 ? (short)0 : Program.GetKeyAtIndex(Program.Count - 1);
        Program[(short)(lastLabel + 1)] = (statement, false);
    }
}