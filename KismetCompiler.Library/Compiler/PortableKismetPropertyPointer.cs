using KismetKompiler.Library.Compiler.Context;
using UAssetAPI.Kismet.Bytecode;

namespace KismetKompiler.Compiler;

public class PortableKismetPropertyPointer : KismetPropertyPointer
{
    public PortableKismetPropertyPointer(Symbol symbol)
    {
        Symbol = symbol;
    }

    public Symbol Symbol { get; }
}
