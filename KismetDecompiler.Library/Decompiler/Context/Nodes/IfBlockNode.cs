using UAssetAPI.Kismet.Bytecode;

namespace KismetCompiler.Library.Decompiler.Context.Nodes;

public class IfBlockNode : BlockNode
{
    public KismetExpression Condition { get; set; } = null!;
}
