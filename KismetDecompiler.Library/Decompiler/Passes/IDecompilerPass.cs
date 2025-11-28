using KismetCompiler.Library.Decompiler.Context;
using KismetCompiler.Library.Decompiler.Context.Nodes;

namespace KismetCompiler.Library.Decompiler.Passes
{
    public interface IDecompilerPass
    {
        Node Execute(DecompilerContext context, Node root);
    }
}
