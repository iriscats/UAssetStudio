using KismetCompiler.Library.Compiler.Context;
using UAssetAPI.Kismet.Bytecode;

namespace KismetCompiler.Library.Compiler.Intermediate;

public class IntermediatePropertyPointer : KismetPropertyPointer
{
    public IntermediatePropertyPointer(Symbol symbol)
    {
        Symbol = symbol;
    }

    public Symbol Symbol { get; }
}
