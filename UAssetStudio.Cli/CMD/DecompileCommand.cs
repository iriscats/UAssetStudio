using System.CommandLine;
using System.IO;
using KismetScript.Decompiler;
using KismetScript.Utilities.Metadata;
using UAssetAPI.UnrealTypes;

namespace UAssetStudio.Cli.CMD
{
    internal static class DecompileCommandBuilder
    {
        internal static Command Create(Option<EngineVersion> ueVersion, Option<string?> mappings)
        {
            var assetArg = new Argument<string>("asset", description: "Path to asset (.uasset/.umap)");
            var outdirOpt = new Option<string?>("--outdir", description: "Output directory; default = asset directory");
            var metaOpt = new Option<bool>("--meta", () => false, "Generate .kms.meta file for standalone compilation");
            var decompile = new Command("decompile", "Decompile .uasset/.umap to .kms")
            {
                assetArg,
                outdirOpt,
                metaOpt
            };

            decompile.AddOption(ueVersion);
            decompile.AddOption(mappings);

            decompile.SetHandler((EngineVersion ver, string? mapPath, string assetPath, string? outdir, bool generateMeta) =>
            {
                var asset = CliHelpers.LoadAsset(ver, mapPath, assetPath);
                var dir = outdir ?? Path.GetDirectoryName(assetPath) ?? ".";
                var kmsPath = Path.Join(dir, Path.ChangeExtension(Path.GetFileName(assetPath), ".kms"));
                CliHelpers.DecompileToKms(asset, kmsPath);
                Console.WriteLine($"Decompiled: {assetPath} -> {kmsPath}");

                if (generateMeta)
                {
                    var metaPath = kmsPath + ".meta";
                    var extractor = new MetadataExtractor();
                    var metadata = extractor.Extract(asset);
                    KmsMetadataSerializer.WriteToFile(metadata, metaPath);
                    Console.WriteLine($"Generated metadata: {metaPath}");
                }
            }, ueVersion, mappings, assetArg, outdirOpt, metaOpt);

            return decompile;
        }
    }
}
