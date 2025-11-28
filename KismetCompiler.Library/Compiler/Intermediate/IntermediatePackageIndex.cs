using KismetCompiler.Library.Compiler.Context;
using UAssetAPI.UnrealTypes;

namespace KismetCompiler.Library.Compiler.Intermediate;

public class IntermediatePackageIndex : FPackageIndex
{
    public IntermediatePackageIndex(Symbol symbol)
    {
        Symbol = symbol;
    }

    public Symbol Symbol { get; }
}
