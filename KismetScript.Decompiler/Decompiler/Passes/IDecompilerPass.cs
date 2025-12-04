using KismetScript.Linker.Decompiler.Context;
using KismetScript.Linker.Decompiler.Context.Nodes;

namespace KismetScript.Linker.Decompiler.Passes
{
    public interface IDecompilerPass
    {
        Node Execute(DecompilerContext context, Node? root);
    }
}
