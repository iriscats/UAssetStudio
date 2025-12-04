namespace KismetScript.Syntax;

internal static class KeywordDictionary
{
    private static readonly Dictionary<ValueKind, string> _valueTypeToKeyword = new()
    {
        { ValueKind.Void, "void" },
        { ValueKind.Bool, "bool" },
        { ValueKind.Byte, "byte" },
        { ValueKind.Int, "int" },
        { ValueKind.Float, "float" },
        { ValueKind.String, "string" }
    };

    private static readonly Dictionary<string, ValueKind> _keywordToValueType = new()
    {
        { "void", ValueKind.Void },
        { "bool", ValueKind.Bool },
        { "byte", ValueKind.Byte },
        { "int", ValueKind.Int },
        { "float", ValueKind.Float },
        { "string", ValueKind.String }
    };

    public static IReadOnlyDictionary<ValueKind, string> ValueTypeToKeyword => _valueTypeToKeyword;
    public static IReadOnlyDictionary<string, ValueKind> KeywordToValueType => _keywordToValueType;
}
