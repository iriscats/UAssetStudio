using UAssetAPI;
using UAssetAPI.UnrealTypes;

namespace KismetCompiler.Library.Packaging;

public record ImportIndex(
    Import Import,
    FPackageIndex Index);
