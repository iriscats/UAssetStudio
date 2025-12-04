namespace KismetScript.Syntax;

public class SourceInfo
{
    public int Line { get; }

    public int Column { get; }

    public string FileName { get; }

    public SourceInfo(int lineIndex, int characterIndex, string fileName)
    {
        Line = lineIndex;
        Column = characterIndex;
        FileName = fileName;
    }

    public override string ToString()
    {
        return $"{FileName} ({Line}:{Column})";
    }
}