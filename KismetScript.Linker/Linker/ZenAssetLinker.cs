using Kismet.Compiler.Compiler.Context;
using Kismet.Compiler.Packaging;
using UAssetAPI.IO;
using UAssetAPI.UnrealTypes;

namespace KismetCompiler.Library.Linker;

public class ZenAssetLinker : PackageLinker<ZenAsset>
{
    public ZenAssetLinker(ZenAsset asset) : base(asset) { }

    protected override ZenAsset CreateDefaultAsset()
    {
        throw new NotImplementedException();
    }

    protected override FPackageIndex CreateProcedureImport(ProcedureSymbol symbol)
    {
        throw new NotImplementedException();
    }

    protected override FPackageIndex EnsureObjectImported(FPackageIndex parent, string objectName, string className, bool bImportOptional = false)
    {
        throw new NotImplementedException();
    }

    protected override FPackageIndex EnsurePackageImported(string objectName, bool bImportOptional = false)
    {
        throw new NotImplementedException();
    }

    protected override IEnumerable<(object ImportOrExport, FPackageIndex PackageIndex)> GetPackageIndexByFullName(string name)
    {
        throw new NotImplementedException();
    }

    protected override IEnumerable<(object ImportOrExport, FPackageIndex PackageIndex)> GetPackageIndexByLocalName(string name)
    {
        throw new NotImplementedException();
    }
}
