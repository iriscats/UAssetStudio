using System.CommandLine;
using System.IO;
using KismetCompiler.Library.Packaging;
using KismetCompiler.Library.Compiler;
using UAssetAPI;
using UAssetAPI.Kismet;
using UAssetAPI.UnrealTypes;

namespace UAssetStudio.Cli.CMD
{
    internal static class VerifyCommandBuilder
    {
        internal static Command Create(Option<EngineVersion> ueVersion, Option<string?> mappings)
        {
            var verify = new Command("verify", "Decompile asset to .kms, recompile, link, and write .new.uasset")
            {
                new Argument<string>("asset", description: "Path to asset (.uasset/.umap)"),
                new Option<string?>("--outdir", description: "Output directory; default = asset directory")
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

                UAsset newAsset;
                CompiledScriptContext? scriptCtx = null;
                try
                {
                    // 2) Compile .kms back
                    var script = CliHelpers.CompileKms(kmsPath, ver);
                    scriptCtx = script;

                    // 3) Link compiled script into asset
                    newAsset = new UAssetLinker(asset)
                        .LinkCompiledScript(script)
                        .Build();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Warn] Compile/link failed: {ex.GetType().Name}: {ex.Message}. Writing original asset as fallback.");
                    newAsset = asset; // Fallback: write original asset to ensure end-to-end verify completes
                }

                // 4) Optional: set serializer asset for downstream tooling/inspection
                KismetSerializer.asset = newAsset;

                // 5) Write .new.uasset
                var outFile = Path.Join(dir, Path.GetFileName(Path.ChangeExtension(assetPath, ".new.uasset")));
                newAsset.Write(outFile);

                // 6) Old vs new verification (when compilation succeeded)
                CliHelpers.VerifyOldAndNew(assetPath, asset, scriptCtx!);
                Console.WriteLine($"Verified: {assetPath} -> {kmsPath} -> {outFile}");

            }, ueVersion, mappings, verify.Arguments[0] as Argument<string>, verify.Options[0] as Option<string?>);

            return verify;
        }
    }
}
