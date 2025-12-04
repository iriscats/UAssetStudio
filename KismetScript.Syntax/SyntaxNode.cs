namespace KismetScript.Syntax;

public abstract class SyntaxNode
{
    public SourceInfo? SourceInfo { get; set; }

    public override string ToString()
    {
        if (SourceInfo != null)
        {
            return SourceInfo.ToString();
        }

        return string.Empty;
    }
}
