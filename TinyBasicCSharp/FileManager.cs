namespace TinyBasicCSharp;

/// <summary>
/// A class to work with files
/// </summary>
public static class FileManager
{
    /// <summary>
    /// Checks if the path is a valid *.bas path.
    /// </summary>
    /// <param name="path">Path to *.bas file. Can be relative path.</param>
    /// <returns>true if valid, false otherwise</returns>
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
    
    /// <summary>
    /// Tries to write the array of lines to the file.
    /// </summary>
    /// <param name="lines">Lines to write</param>
    /// <param name="path">Path to write lines</param>
    /// <param name="overwriteWithoutRequest">If true, doesn't request a confirmation from the user to overwrite an existing file</param>
    /// <returns>Saving status</returns>
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
        /// <summary>
        /// Saved successfully
        /// </summary>
        Success,
        /// <summary>
        /// An error occured while saving file
        /// </summary>
        Error,
        /// <summary>
        /// Saving cancelled because user did not confirm overwrite request
        /// </summary>
        Aborted
    }

    /// <summary>
    /// Tries to read a file.
    /// </summary>
    /// <param name="path">Path to the file</param>
    /// <returns>null if path doesn't exist; array of lines otherwise</returns>
    public static string[]? ReadFile(string path)
    {
        if (string.IsNullOrEmpty(path))
        { throw new ArgumentException("Path cannot be null or empty"); }

        return File.Exists(path) ? File.ReadAllLines(path) : null;
    }
}