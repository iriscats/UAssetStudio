namespace KismetCompiler.Library.Decompiler.Context.Nodes;

public class JumpNode : Node
{
    public Node Target { get; set; } = null!;

    public override string ToString()
    {
        return $"{CodeStartOffset}: {Source?.Inst} -> {Target}";
    }
}
