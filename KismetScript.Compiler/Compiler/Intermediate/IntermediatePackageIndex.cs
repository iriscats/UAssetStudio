using KismetScript.Compiler.Compiler.Context;
using UAssetAPI.UnrealTypes;

namespace KismetScript.Compiler.Compiler.Intermediate;

public class IntermediatePackageIndex : FPackageIndex
{
    public IntermediatePackageIndex(Symbol symbol)
    {
        Symbol = symbol;
    }

    public Symbol Symbol { get; }
}
