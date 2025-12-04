using System.CommandLine;
using System.IO;
using UAssetAPI.UnrealTypes;

namespace UAssetStudio.Cli.CMD
{
    internal static class DecompileCommandBuilder
    {
        internal static Command Create(Option<EngineVersion> ueVersion, Option<string?> mappings)
        {
            var assetArg = new Argument<string>("asset", description: "Path to asset (.uasset/.umap)");
            var outdirOpt = new Option<string?>("--outdir", description: "Output directory; default = asset directory");
            var decompile = new Command("decompile", "Decompile .uasset/.umap to .kms")
            {
                assetArg,
                outdirOpt
            };

            decompile.AddOption(ueVersion);
            decompile.AddOption(mappings);

            decompile.SetHandler((EngineVersion ver, string? mapPath, string assetPath, string? outdir) =>
            {
                var asset = CliHelpers.LoadAsset(ver, mapPath, assetPath);
                var dir = outdir ?? Path.GetDirectoryName(assetPath) ?? ".";
                var kmsPath = Path.Join(dir, Path.ChangeExtension(Path.GetFileName(assetPath), ".kms"));
                CliHelpers.DecompileToKms(asset, kmsPath);
                Console.WriteLine($"Decompiled: {assetPath} -> {kmsPath}");
            }, ueVersion, mappings, assetArg, outdirOpt);

            return decompile;
        }
    }
}
