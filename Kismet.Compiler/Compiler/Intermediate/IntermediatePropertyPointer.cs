using Kismet.Compiler.Compiler.Context;
using UAssetAPI.Kismet.Bytecode;

namespace Kismet.Compiler.Compiler.Intermediate;

public class IntermediatePropertyPointer : KismetPropertyPointer
{
    public IntermediatePropertyPointer(Symbol symbol)
    {
        Symbol = symbol;
    }

    public Symbol Symbol { get; }
}
