using KismetScript.Compiler.Compiler.Context;
using UAssetAPI.Kismet.Bytecode;

namespace KismetScript.Compiler.Compiler.Intermediate;

public class IntermediatePropertyPointer : KismetPropertyPointer
{
    public IntermediatePropertyPointer(Symbol symbol)
    {
        Symbol = symbol;
    }

    public Symbol Symbol { get; }
}
