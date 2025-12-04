using UAssetAPI.UnrealTypes;

namespace KismetScript.Compiler.Compiler.Intermediate;

public class IntermediateName : FName
{
    public IntermediateName(string value)
    {
        TextValue = value;
    }

    public string TextValue { get; }
}
