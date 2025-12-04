namespace Kismet.Compiler.Syntax.Statements.Expressions;

public abstract class BinaryExpression : Expression
{
    public Expression Left { get; set; } = null!;

    public Expression Right { get; set; } = null!;

    protected BinaryExpression(ValueKind kind) : base(kind)
    {
    }

    protected BinaryExpression(ValueKind kind, Expression left, Expression right) : this(kind)
    {
        Left = left;
        Right = right;
    }

    public override int GetDepth() => 1 + Left.GetDepth() + Right.GetDepth();
}
