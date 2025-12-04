using UAssetAPI.Kismet.Bytecode;

namespace Kismet.Compiler.Utilities;

public record KismetExpressionContext<T>(
    KismetExpression Expression,
    int CodeStartOffset,
    T? Tag)
{
    public int? CodeEndOffset { get; set; }
}
