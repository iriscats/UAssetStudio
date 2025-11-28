using UAssetAPI.UnrealTypes;

namespace KismetCompiler.Library.Compiler.Intermediate;

public class IntermediateName : FName
{
    public IntermediateName(string value)
    {
        TextValue = value;
    }

    public string TextValue { get; }
}
