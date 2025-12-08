using KismetScript.Compiler.Compiler.Context;
using UAssetAPI.Kismet.Bytecode;
using UAssetAPI.UnrealTypes;

namespace KismetScript.Compiler.Compiler.Intermediate;

public class IntermediatePropertyPointer : KismetPropertyPointer
{
    public IntermediatePropertyPointer(Symbol symbol)
    {
        Symbol = symbol;
    }

    public Symbol Symbol { get; }

    /// <summary>
    /// Original ResolvedOwner from decompilation, used when symbol cannot be fully resolved during compilation
    /// </summary>
    public FPackageIndex? OriginalResolvedOwner { get; set; }
}
