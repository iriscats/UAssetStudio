using System.CommandLine;
using UAssetAPI;
using UAssetAPI.UnrealTypes;
using UAssetAPI.Unversioned;

namespace UAssetStudio.Cli.CMD;

internal static class JsonCommandBuilder
{
    internal static Command Create(Option<EngineVersion> ueVersion, Option<string?> mappings)
    {
        var inputArg = new Argument<string>("input", description: "Path to .uasset/.umap/.json");
        var outputOpt = new Option<string?>("--out", description: "Output file; default is derived from the input path");

        var json = new Command("json", "Convert assets between binary (.uasset/.umap) and JSON")
        {
            inputArg,
            outputOpt,
        };

        json.AddOption(ueVersion);
        json.AddOption(mappings);

        json.SetHandler((EngineVersion version, string? mappingsPath, string inputPath, string? outputPath) =>
        {
            if (!File.Exists(inputPath))
            {
                Console.Error.WriteLine($"Input file not found: {inputPath}");
                Environment.ExitCode = 1;
                return;
            }

            var extension = Path.GetExtension(inputPath).ToLowerInvariant();
            var isAsset = extension == ".uasset" || extension == ".umap";
            var isJson = extension == ".json";

            if (!isAsset && !isJson)
            {
                Console.Error.WriteLine($"Unsupported input extension: {extension}. Expected .uasset, .umap, or .json.");
                Environment.ExitCode = 1;
                return;
            }

            try
            {
                if (isAsset)
                {
                    ConvertAssetToJson(version, mappingsPath, inputPath, outputPath);
                }
                else
                {
                    ConvertJsonToAsset(mappingsPath, inputPath, outputPath);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Conversion failed: {ex.GetType().Name}: {ex.Message}");
                Environment.ExitCode = 1;
            }
        }, ueVersion, mappings, inputArg, outputOpt);

        return json;
    }

    private static void ConvertAssetToJson(EngineVersion version, string? mappingsPath, string inputPath, string? outputPath)
    {
        var asset = CliHelpers.LoadAsset(version, mappingsPath, inputPath);
        var defaultOutput = Path.Join(Path.GetDirectoryName(inputPath) ?? ".", Path.GetFileName(inputPath) + ".json");
        var outPath = outputPath ?? defaultOutput;
        EnsureOutputDirectory(outPath);
        var json = asset.SerializeJson(true);
        File.WriteAllText(outPath, json);
        Console.WriteLine($"Exported JSON: {outPath}");
    }

    private static void ConvertJsonToAsset(string? mappingsPath, string inputPath, string? outputPath)
    {
        var jsonText = File.ReadAllText(inputPath);
        var asset = UAsset.DeserializeJson(jsonText);

        if (!string.IsNullOrEmpty(mappingsPath))
        {
            if (!File.Exists(mappingsPath))
            {
                throw new FileNotFoundException("Mappings file not found", mappingsPath);
            }
            asset.Mappings = new Usmap(mappingsPath);
        }

        var baseName = Path.GetFileNameWithoutExtension(inputPath);
        if (!baseName.EndsWith(".uasset", StringComparison.OrdinalIgnoreCase) &&
            !baseName.EndsWith(".umap", StringComparison.OrdinalIgnoreCase))
        {
            baseName += ".uasset";
        }

        var defaultOutput = Path.Join(Path.GetDirectoryName(inputPath) ?? ".", baseName);
        var outPath = outputPath ?? defaultOutput;
        EnsureOutputDirectory(outPath);
        asset.Write(outPath);
        Console.WriteLine($"Generated asset: {outPath}");
    }

    private static void EnsureOutputDirectory(string path)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }
}
