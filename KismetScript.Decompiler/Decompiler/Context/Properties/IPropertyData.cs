using UAssetAPI;
using UAssetAPI.UnrealTypes;

namespace KismetScript.Linker.Decompiler.Context.Properties;

public interface IPropertyData
{
    UnrealPackage Asset { get; }
    object Source { get; }

    string Name { get; }
    EPropertyFlags PropertyFlags { get; }
    string TypeName { get; }

    string? PropertyClassName { get; }
    string? InterfaceClassName { get; }
    string? StructName { get; }
    IPropertyData? ArrayInnerProperty { get; }
}
