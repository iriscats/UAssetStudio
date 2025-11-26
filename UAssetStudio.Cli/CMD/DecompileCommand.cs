using System.CommandLine;
using System.IO;
using UAssetAPI.UnrealTypes;

namespace UAssetStudio.Cli.CMD
{
    internal static class DecompileCommandBuilder
    {
        internal static Command Create(Option<EngineVersion> ueVersion, Option<string?> mappings)
        {
            var decompile = new Command("decompile", "Decompile .uasset/.umap to .kms")
            {
                new Argument<string>("asset", description: "Path to asset (.uasset/.umap)"),
                new Option<string?>("--outdir", description: "Output directory; default = asset directory")
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
            }, ueVersion, mappings, decompile.Arguments[0] as Argument<string>, decompile.Options[0] as Option<string?>);

            return decompile;
        }
    }
}
