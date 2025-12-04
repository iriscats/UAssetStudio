namespace Kismet.Compiler.Syntax.Statements.Expressions;

public abstract class UnaryExpression : Expression
{
    public Expression Operand { get; set; } = null!;

    protected UnaryExpression(ValueKind kind) : base(kind)
    {
    }

    protected UnaryExpression(ValueKind kind, Expression operand) : base(kind)
    {
        Operand = operand;
    }

    public override int GetDepth()
    {
        return 1 + Operand.GetDepth();
    }
}
