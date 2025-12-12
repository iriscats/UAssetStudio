namespace KismetScript.Syntax.Statements.Expressions.Literals;

public class UInt32Literal : Literal<uint>, IEquatable<UInt32Literal>
{
    public UInt32Literal() : base(ValueKind.UInt32)
    {
    }

    public UInt32Literal(uint value) : base(ValueKind.UInt32, value)
    {
    }

    public bool Equals(UInt32Literal? other)
    {
        return Value == other?.Value;
    }

    public static implicit operator UInt32Literal(uint value) => new UInt32Literal(value);
}
