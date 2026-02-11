using System.Text;
using KismetScript.Syntax.Statements.Expressions.Literals;

namespace KismetScript.Syntax.Statements.Expressions;

/// <summary>
/// Represents a key-value pair in an object literal expression.
/// Key can be an Identifier, StringLiteral, or nested ObjectLiteral (for map struct keys).
/// </summary>
public class ObjectLiteralEntry
{
    public Expression Key { get; set; } = null!;
    public Expression Value { get; set; } = null!;

    /// <summary>
    /// Gets the string representation of the key for dictionary lookups.
    /// </summary>
    public string KeyText => Key switch
    {
        Identifier id => id.Text,
        StringLiteral str => str.Value,
        _ => Key.ToString() ?? ""
    };
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
