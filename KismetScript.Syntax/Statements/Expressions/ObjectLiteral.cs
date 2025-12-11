using System.Text;

namespace KismetScript.Syntax.Statements.Expressions;

/// <summary>
/// Represents a key-value pair in an object literal expression.
/// </summary>
public class ObjectLiteralEntry
{
    public Identifier Key { get; set; } = null!;
    public Expression Value { get; set; } = null!;
}

/// <summary>
/// Represents an object literal expression with key-value pairs: { key1: value1, key2: value2 }
/// Used for Map and Struct literals.
/// </summary>
public class ObjectLiteral : Expression
{
    public List<ObjectLiteralEntry> Entries { get; set; }

    public ObjectLiteral() : base(ValueKind.Unresolved)
    {
        Entries = new List<ObjectLiteralEntry>();
    }

    public override string ToString()
    {
        var builder = new StringBuilder();

        builder.Append("{ ");

        if (Entries.Count > 0)
            builder.Append($"{Entries[0].Key}: {Entries[0].Value}");

        for (int i = 1; i < Entries.Count; i++)
        {
            builder.Append($", {Entries[i].Key}: {Entries[i].Value}");
        }

        builder.Append(" }");

        return builder.ToString();
    }

    public override int GetDepth() => 1;
}
