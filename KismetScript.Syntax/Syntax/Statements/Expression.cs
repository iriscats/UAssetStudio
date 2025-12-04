namespace KismetScript.Syntax.Statements;

public abstract class Expression : Statement
{
    public virtual ValueKind ExpressionValueKind { get; set; }

    protected Expression(ValueKind kind)
    {
        ExpressionValueKind = kind;
    }

    public abstract int GetDepth();
}