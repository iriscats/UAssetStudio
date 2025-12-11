using KismetScript.Compiler.Compiler.Context;
using UAssetAPI.Kismet.Bytecode;
using UAssetAPI.UnrealTypes;

namespace KismetScript.Compiler.Compiler;

public abstract class CompiledDeclarationContext
{
    public virtual Symbol Symbol { get; }

    public CompiledDeclarationContext(Symbol symbol)
    {
        Symbol = symbol;
    }
}

public class CompiledDeclarationContext<T> : CompiledDeclarationContext where T : Symbol
{
    public CompiledDeclarationContext(T symbol) : base(symbol)
    {
        Symbol = symbol;
    }

    public override T Symbol { get; }
}

public class CompiledLabelContext : CompiledDeclarationContext<LabelSymbol>
{
    public CompiledLabelContext(LabelSymbol symbol) : base(symbol) { }
    public int CodeOffset { get; set; }
}

public class CompiledFunctionContext : CompiledDeclarationContext<ProcedureSymbol>
{
    public CompiledFunctionContext(ProcedureSymbol symbol) : base(symbol) { }
    public List<CompiledVariableContext> Variables { get; init; } = new();
    public List<CompiledLabelContext> Labels { get; init; } = new();
    public List<KismetExpression> Bytecode { get; init; } = new();
}

public class CompiledVariableContext : CompiledDeclarationContext<VariableSymbol>
{
    public CompiledVariableContext(VariableSymbol symbol) : base(symbol) { }

    public CompiledClassContext Type { get; set; }

    /// <summary>
    /// The compiled initializer value for this variable (for CDO properties).
    /// </summary>
    public CompiledPropertyValue? Initializer { get; set; }
}

public class CompiledClassContext : CompiledDeclarationContext<ClassSymbol>
{
    public CompiledClassContext(ClassSymbol symbol) : base(symbol) { }
    public EClassFlags Flags { get; set; }
    public CompiledClassContext? BaseClass { get; set; }
    public List<CompiledVariableContext> Variables { get; init; } = new();
    public List<CompiledFunctionContext> Functions { get; init; } = new();
}

public class CompiledImportContext : CompiledDeclarationContext<PackageSymbol>
{
    public CompiledImportContext(PackageSymbol symbol) : base(symbol) { }
    public List<CompiledDeclarationContext> Declarations { get; init; } = new();
}

public class CompiledScriptContext
{
    public List<CompiledImportContext> Imports { get; init; } = new();
    public List<CompiledVariableContext> Variables { get; init; } = new();
    public List<CompiledFunctionContext> Functions { get; init; } = new();
    public List<CompiledClassContext> Classes { get; init; } = new();
    public List<CompiledObjectContext> Objects { get; init; } = new();
}

/// <summary>
/// Represents a compiled object declaration (sub-object with property values).
/// </summary>
public class CompiledObjectContext
{
    /// <summary>
    /// The name of the object instance.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The class/type of this object.
    /// </summary>
    public string ClassName { get; set; } = string.Empty;

    /// <summary>
    /// Property assignments within the object.
    /// </summary>
    public List<CompiledPropertyAssignment> Properties { get; init; } = new();
}

/// <summary>
/// Represents a compiled property assignment with type, name, and value.
/// </summary>
public class CompiledPropertyAssignment
{
    /// <summary>
    /// The type of the property (e.g., "float", "Object<ClassName>").
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// The name of the property.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The compiled value of the property.
    /// </summary>
    public CompiledPropertyValue? Value { get; set; }
}

/// <summary>
/// Represents a compiled property value that can hold various types.
/// </summary>
public class CompiledPropertyValue
{
    public float? FloatValue { get; set; }
    public double? DoubleValue { get; set; }
    public int? IntValue { get; set; }
    public long? Int64Value { get; set; }
    public bool? BoolValue { get; set; }
    public byte? ByteValue { get; set; }
    public string? StringValue { get; set; }
    public string? ObjectReference { get; set; }
    public List<CompiledPropertyValue>? ArrayValue { get; set; }
    public Dictionary<string, CompiledPropertyValue>? StructValue { get; set; }
}
