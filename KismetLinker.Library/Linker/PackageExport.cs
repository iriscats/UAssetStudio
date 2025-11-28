using UAssetAPI.ExportTypes;
using UAssetAPI.UnrealTypes;

namespace KismetCompiler.Library.Packaging;

public record PackageExport<T>(FPackageIndex Index, T Export) where T : Export;
