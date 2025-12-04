using UAssetAPI.Kismet.Bytecode;

namespace KismetScript.Linker.Decompiler.Context.Nodes;

public class IfBlockNode : BlockNode
{
    public KismetExpression Condition { get; set; } = null!;
}
