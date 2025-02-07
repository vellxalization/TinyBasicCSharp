namespace TinyBasicCSharp.Environment;

/// <summary>
/// Class for convenient storing of values 
/// </summary>
public class EnvironmentMemory
{
    private short?[] _memory = new short?[26];

    /// <summary>
    /// Writes a value to the address
    /// </summary>
    /// <param name="value">Value to write</param>
    /// <param name="address">Address to write to</param>
    /// <exception cref="ArgumentException">Address is outside the A-Z range</exception>
    public void WriteVariable(short value, char address)
    {
        if (address is < 'A' or > 'Z')
        { throw new ArgumentException("Invalid memory address"); }
        
        _memory[address - 'A'] = value;
    }

    /// <summary>
    /// Reads a variable from the address
    /// </summary>
    /// <param name="address">Address to read from</param>
    /// <returns>Stored value. Null if value wasn't initialized before</returns>
    /// <exception cref="ArgumentException">Address is outside the A-Z range</exception>
    public short? ReadVariable(char address)
    {
        if (address is < 'A' or > 'Z')
        { throw new ArgumentException("Invalid memory address"); }
        
        return _memory[address - 'A'];
    }

    /// <summary>
    /// Resets all variables to 'uninitialized' (null)
    /// </summary>
    public void Reset() => _memory = new short?[26];
}