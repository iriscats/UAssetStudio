using System.CommandLine;
using UAssetAPI;
using UAssetAPI.UnrealTypes;
using UAssetAPI.Unversioned;
using KismetAnalyzer; 

namespace UAssetStudio.Cli;

public static class ToolMain
{
    static UAsset LoadAsset(EngineVersion ueVersion, string? mappings, string assetPath)
    {
        return mappings != null
            ? new UAsset(assetPath, ueVersion, new Usmap(mappings))
            : new UAsset(assetPath, ueVersion);
    }

    static void GenCfg(EngineVersion ueVersion, string? mappings, string assetPath, string outputDir)
    {
        UAsset asset = LoadAsset(ueVersion, mappings, assetPath);
        var fileName = Path.GetFileName(assetPath);
        var output = new StreamWriter(Path.Join(outputDir, Path.ChangeExtension(fileName, ".txt")));
        var dotOutput = new StreamWriter(Path.Join(outputDir, Path.ChangeExtension(fileName, ".dot")));
        new SummaryGenerator(asset, output, dotOutput).Summarize();
        output.Close();
        dotOutput.Close();
    }

    public static int Main(string[] args)
    {
        var root = new RootCommand("UAssetStudio CLI entrypoint");

        var assetInput = new Argument<string>("input", description: "Path to asset (.uasset/.umap)");
        var outputDir = new Option<string?>("--outdir", description: "Output directory for CFG/dot; default = asset directory");
        var ueVersion = new Option<EngineVersion>("--ue-version", description: "Unreal Engine version", getDefaultValue: () => EngineVersion.VER_UE4_27);
        var mappings = new Option<string?>("--mappings", description: "Path to .usmap for unversioned properties");

        root.AddArgument(assetInput);
        root.AddOption(outputDir);
        root.AddOption(ueVersion);
        root.AddOption(mappings);

        root.SetHandler((EngineVersion ver, string? mapPath, string input, string? outdir) =>
        {
            var dir = outdir ?? Path.GetDirectoryName(input) ?? ".";
            GenCfg(ver, mapPath, input, dir);
        }, ueVersion, mappings, assetInput, outputDir);

        return root.Invoke(args);
    }
}

