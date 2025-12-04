using Kismet.Compiler.Compiler.Context;
using UAssetAPI.UnrealTypes;

namespace Kismet.Compiler.Compiler.Intermediate;

public class IntermediatePackageIndex : FPackageIndex
{
    public IntermediatePackageIndex(Symbol symbol)
    {
        Symbol = symbol;
    }

    public Symbol Symbol { get; }
}
