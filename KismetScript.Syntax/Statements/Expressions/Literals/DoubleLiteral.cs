namespace KismetScript.Syntax.Statements.Expressions.Literals;

public class DoubleLiteral : Literal<double>, IEquatable<DoubleLiteral>
{
    public DoubleLiteral() : base(ValueKind.Double)
    {
    }

    public DoubleLiteral(double value) : base(ValueKind.Double, value)
    {
    }

    public override string ToString()
    {
        return FormattableString.Invariant($"{Value}d");
    }

    public bool Equals(DoubleLiteral? other)
    {
        return Value == other?.Value;
    }

    public static implicit operator DoubleLiteral(double value) => new DoubleLiteral(value);
}
