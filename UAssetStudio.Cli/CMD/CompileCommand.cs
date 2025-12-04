using System.CommandLine;
using System.IO;
using KismetCompiler.Library.Packaging;
using UAssetAPI.UnrealTypes;

namespace UAssetStudio.Cli.CMD
{
    internal static class CompileCommandBuilder
    {
        internal static Command Create(Option<EngineVersion> ueVersion, Option<string?> mappings)
        {
            var scriptArg = new Argument<string>("script", description: "Path to input .kms");
            var assetOpt = new Option<string?>("--asset", description: "Original asset path (.uasset); defaults to script .uasset neighbor");
            var outdirOpt = new Option<string?>("--outdir", description: "Output directory; default = script directory");
            var compile = new Command("compile", "Compile .kms to .uasset")
            {
                scriptArg,
                assetOpt,
                outdirOpt
            };

            compile.AddOption(ueVersion);
            compile.AddOption(mappings);

            compile.SetHandler((EngineVersion ver, string? mapPath, string scriptPath, string? assetPath, string? outdir) =>
            {
                if (!File.Exists(scriptPath))
                {
                    Console.WriteLine($"Error: Input file {scriptPath} does not exist");
                    return;
                }

                var script = CliHelpers.CompileKms(scriptPath, ver);
                var originalAssetPath = assetPath ?? Path.ChangeExtension(scriptPath, ".uasset");
                if (!File.Exists(originalAssetPath))
                {
                    Console.WriteLine($"Error: Cannot find original asset file {originalAssetPath} for compilation");
                    return;
                }

                var asset = CliHelpers.LoadAsset(ver, mapPath, originalAssetPath);
                var newAsset = new UAssetLinker(asset)
                    .LinkCompiledScript(script)
                    .Build();
                var dir = outdir ?? Path.GetDirectoryName(scriptPath) ?? ".";
                var outFile = Path.Join(dir, Path.GetFileName(Path.ChangeExtension(scriptPath, ".compiled.uasset")));
                newAsset.Write(outFile);
                Console.WriteLine($"Compiled: {scriptPath} -> {outFile}");
            }, ueVersion, mappings, scriptArg, assetOpt, outdirOpt);

            return compile;
        }
    }
}
