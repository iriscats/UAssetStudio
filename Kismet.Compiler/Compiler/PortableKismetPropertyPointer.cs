using Kismet.Compiler.Compiler.Context;
using UAssetAPI.Kismet.Bytecode;

namespace KismetCompiler.Compiler;

public class PortableKismetPropertyPointer : KismetPropertyPointer
{
    public PortableKismetPropertyPointer(Symbol symbol)
    {
        Symbol = symbol;
    }

    public Symbol Symbol { get; }
}
