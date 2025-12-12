namespace KismetScript.Syntax.Statements.Expressions.Literals;

public class Int64Literal : Literal<long>, IEquatable<Int64Literal>
{
    public Int64Literal() : base(ValueKind.Int64)
    {
    }

    public Int64Literal(long value) : base(ValueKind.Int64, value)
    {
    }

    public bool Equals(Int64Literal? other)
    {
        return Value == other?.Value;
    }

    public static implicit operator Int64Literal(long value) => new Int64Literal(value);
}
