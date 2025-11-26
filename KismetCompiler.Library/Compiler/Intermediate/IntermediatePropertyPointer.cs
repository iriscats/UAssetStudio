using KismetKompiler.Library.Compiler.Context;
using UAssetAPI.Kismet.Bytecode;

namespace KismetKompiler.Library.Compiler.Intermediate;

public class IntermediatePropertyPointer : KismetPropertyPointer
{
    public IntermediatePropertyPointer(Symbol symbol)
    {
        Symbol = symbol;
    }

    public Symbol Symbol { get; }
}
