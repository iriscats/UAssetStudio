using System.CommandLine;
using KismetScript.Linker;
using UAssetAPI.UnrealTypes;

namespace UAssetStudio.Cli.CMD
{
    internal static class VerifyCommandBuilder
    {
        internal static Command Create(Option<EngineVersion> ueVersion, Option<string?> mappings)
        {
            var assetArg = new Argument<string>("asset", description: "Path to asset (.uasset/.umap)");
            var outdirOpt = new Option<string?>("--outdir", description: "Output directory; default = asset directory");
            var verify = new Command("verify", "Decompile asset to .kms, recompile, link, and write .new.uasset")
            {
                assetArg,
                outdirOpt
            };

            verify.AddOption(ueVersion);
            verify.AddOption(mappings);

            verify.SetHandler((EngineVersion ver, string? mapPath, string assetPath, string? outdir) =>
            {
                if (!File.Exists(assetPath))
                {
                    Console.WriteLine($"Error: Input file {assetPath} does not exist");
                    return;
                }

                var asset = CliHelpers.LoadAsset(ver, mapPath, assetPath);
                var dir = outdir ?? Path.GetDirectoryName(assetPath) ?? ".";

                // 1) Decompile to .kms
                var kmsPath = Path.Join(dir, Path.ChangeExtension(Path.GetFileName(assetPath), ".kms"));
                CliHelpers.DecompileToKms(asset, kmsPath);

                // 2) Compile .kms back
                var script = CliHelpers.CompileKms(kmsPath, ver);

                // 3) Link compiled script into asset
                var newAsset = new UAssetLinker(asset)
                        .LinkCompiledScript(script)
                        .Build();

                // 5) Write .new.uasset
                var outFile = Path.Join(dir, Path.GetFileName(Path.ChangeExtension(assetPath, ".new.uasset")));
                newAsset.Write(outFile);

                // 6) Old vs new verification: compare full JSON via file paths
                CliHelpers.VerifyOldAndNewExport(assetPath, outFile, ver, mapPath);
                Console.WriteLine($"Verified: {assetPath} -> {kmsPath} -> {outFile}");

            }, ueVersion, mappings, assetArg, outdirOpt);

            return verify;
        }
    }
}
