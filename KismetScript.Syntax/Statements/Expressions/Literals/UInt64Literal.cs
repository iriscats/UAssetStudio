namespace KismetScript.Syntax.Statements.Expressions.Literals;

public class UInt64Literal : Literal<ulong>, IEquatable<UInt64Literal>
{
    public UInt64Literal() : base(ValueKind.UInt64)
    {
    }

    public UInt64Literal(ulong value) : base(ValueKind.UInt64, value)
    {
    }

    public bool Equals(UInt64Literal? other)
    {
        return Value == other?.Value;
    }

    public static implicit operator UInt64Literal(ulong value) => new UInt64Literal(value);
}
