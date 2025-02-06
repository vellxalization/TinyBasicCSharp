namespace TinyBasicCSharp;

public static class FileManager
{
    public static bool IsValidBasPath(string path)
    {
        if (string.IsNullOrEmpty(path))
        { return false; }
        
        if (!(Uri.TryCreate(path, UriKind.RelativeOrAbsolute, out var uriResult)))
        { return false; }
       
        if (!uriResult.IsAbsoluteUri)
        { return path.EndsWith(".bas"); }

        return uriResult.Scheme == Uri.UriSchemeFile && path.EndsWith(".bas");
    }
    
    public static SaveStatus SaveTo(string[] lines, string path, bool overwriteWithoutRequest = false)
    {
        if (string.IsNullOrEmpty(path))
        { throw new ArgumentException("Path cannot be null or empty"); }

        FileStream? stream;
        if (File.Exists(path))
        {
            if (!overwriteWithoutRequest)
            {
                Console.WriteLine($"File {path} already exists. Overwrite? (y)es or (n)o");
                if (!ConsoleInterface.RequestConfirmation())
                { return SaveStatus.Aborted; }
            }
            
            try
            { stream = File.Open(path, FileMode.Truncate, FileAccess.Write); }
            catch (Exception e)
            {
                Console.WriteLine($"Error opening file: {e.Message}");
                return SaveStatus.Error;
            }
        }
        else
        {
            try
            { stream = File.Open(path, FileMode.Create, FileAccess.Write); }
            catch (Exception e)
            {
                Console.WriteLine($"Error opening file: {e.Message}");
                return SaveStatus.Error;
            }
        }
        
        using var writer = new StreamWriter(stream);
        foreach (var line in lines)
        { writer.WriteLine(line); }
        
        writer.Flush();
        return SaveStatus.Success;
    }

    public enum SaveStatus
    {
        Success,
        Error,
        Aborted
    }

    public static string[]? ReadFile(string path)
    {
        if (string.IsNullOrEmpty(path))
        { throw new ArgumentException("Path cannot be null or empty"); }

        return File.Exists(path) ? File.ReadAllLines(path) : null;
    }
}