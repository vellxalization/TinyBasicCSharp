namespace TinyCompilerForTinyBasic.Environment;

public class EnvironmentMemory
{
    private short?[] _memory = new short?[26];

    public void WriteVariable(short value, char address)
    {
        if (address is < 'A' or > 'Z')
        { throw new ArgumentException("Invalid memory address"); }
        
        _memory[address - 'A'] = value;
    }

    public short? ReadVariable(char address)
    {
        if (address is < 'A' or > 'Z')
        { throw new ArgumentException("Invalid memory address"); }
        
        return _memory[address - 'A'];
    }
}