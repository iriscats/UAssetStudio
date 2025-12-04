namespace KismetScript.Syntax.Statements;

public class SwitchStatement : Statement
{
    public Expression SwitchOn { get; set; } = null!;

    public List<SwitchLabel> Labels { get; set; }

    public SwitchStatement()
    {
        Labels = new List<SwitchLabel>();
    }

    public SwitchStatement(Expression switchOn, params SwitchLabel[] labels)
    {
        SwitchOn = switchOn;
        Labels = labels.ToList();
    }

    public override string ToString()
    {
        return $"switch ( {SwitchOn} ) {{ ... }}";
    }
}
