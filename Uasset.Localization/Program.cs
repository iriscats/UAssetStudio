using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.PropertyTypes.Structs;
using UAssetAPI.UnrealTypes;
using UAssetAPI.Unversioned;

namespace Uasset.Localization;

public class Program
{
    public static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("UAsset.Localization - Localize Unreal Engine assets By Iris");
            Console.WriteLine("Usage: Uasset.Localization <asset_or_directory> [--ue-version <version>] [--mappings <usmap_path>] [--out <output_json>] ");
            Console.WriteLine("Example: Uasset.Localization Artifact_RatBastard.uasset --ue-version VER_UE5_6");
            return 1;
        }

        string targetPath = args[0];
        string? ueVersionArg = GetOption(args, "--ue-version");
        string? mappingsPath = GetOption(args, "--mappings");
        string? outPath = GetOption(args, "--out");

        EngineVersion ueVersion = ParseEngineVersion(ueVersionArg) ?? EngineVersion.VER_UE4_27;

        var results = new List<LocalizationEntry>();
        Console.Error.WriteLine($"[INFO] Loading mappings: {mappingsPath ?? "(none)"}");
        Usmap? mappings = !string.IsNullOrEmpty(mappingsPath) ? new Usmap(mappingsPath) : null;
        Console.Error.WriteLine("[INFO] Mappings loaded successfully");

        if (Directory.Exists(targetPath))
        {
            var assetFiles = EnumerateAssets(targetPath).ToList();
            Console.Error.WriteLine($"[INFO] Found {assetFiles.Count} assets in {targetPath}");
            int processed = 0;
            int failed = 0;
            foreach (var assetFile in assetFiles)
            {
                try
                {
                    ExtractFromAsset(assetFile, ueVersion, mappings, results);
                    processed++;
                    if (processed % 50 == 0)
                    {
                        Console.Error.WriteLine($"[INFO] Processed {processed}/{assetFiles.Count} assets, found {results.Count} entries");
                    }
                }
                catch (Exception ex)
                {
                    failed++;
                    Console.Error.WriteLine($"[WARN] Failed to process {Path.GetFileName(assetFile)}: {ex.Message}");
                }
            }
            Console.Error.WriteLine($"[INFO] Completed: {processed} succeeded, {failed} failed, {results.Count} entries found");
        }
        else if (File.Exists(targetPath))
        {
            Console.Error.WriteLine($"[INFO] Processing single file: {targetPath}");
            if (IsJsonAsset(targetPath))
            {
                ExtractFromJsonAsset(targetPath, results);
            }
            else
            {
                ExtractFromAsset(targetPath, ueVersion, mappings, results);
            }
            Console.Error.WriteLine($"[INFO] Found {results.Count} entries");
        }
        else
        {
            Console.Error.WriteLine($"Path not found: {targetPath}");
            return 1;
        }

        var simplified = results
            .Where(r => !string.IsNullOrEmpty(r.source) && !string.IsNullOrEmpty(r.value))
            .Select(r => new OutputEntry { asset = r.asset, text = r.source!, hash = r.value! })
            .ToList();

        Console.Error.WriteLine($"[INFO] Writing {simplified.Count} entries to output");
        if (!string.IsNullOrEmpty(outPath))
        {
            var outDir = Path.GetDirectoryName(outPath);
            if (!string.IsNullOrEmpty(outDir))
            {
                Directory.CreateDirectory(outDir);
            }
            var json = JsonSerializer.Serialize(simplified, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(outPath, json);
            Console.Error.WriteLine($"[INFO] Output written to: {outPath}");
        }
        else
        {
            foreach (var r in simplified)
            {
                Console.WriteLine($"{r.asset}\t{r.text}\t{r.hash}");
            }
        }

        return 0;
    }

    private static string? GetOption(string[] args, string name)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == name) return args[i + 1];
        }
        return null;
    }

    private static IEnumerable<string> EnumerateAssets(string directory)
    {
        var enumOptions = new EnumerationOptions { IgnoreInaccessible = true, RecurseSubdirectories = true };
        return new[] { "*.uasset", "*.umap", "*.uasset.json" }.SelectMany(pattern => Directory.EnumerateFiles(directory, pattern, enumOptions));
    }

    private static bool IsJsonAsset(string path)
    {
        var ext = Path.GetExtension(path)?.ToLowerInvariant();
        return ext == ".json" || path.EndsWith(".uasset.json", StringComparison.OrdinalIgnoreCase);
    }

    private static EngineVersion? ParseEngineVersion(string? v)
    {
        if (string.IsNullOrEmpty(v)) return null;
        if (Enum.TryParse<EngineVersion>(v, out var ev)) return ev;
        return null;
    }

    private static void ExtractFromAsset(string assetPath, EngineVersion ueVersion, Usmap? mappings, List<LocalizationEntry> sink)
    {
        UAsset asset = mappings != null ? new UAsset(assetPath, ueVersion, mappings) : new UAsset(assetPath, ueVersion);

        foreach (var export in asset.Exports)
        {
            if (export is NormalExport ne)
            {
                for (int i = 0; i < ne.Data.Count; i++)
                {
                    var prop = ne.Data[i];
                    TraverseProperty(assetPath, export, prop, prop.Name.ToString(), sink);
                }
            }
            else if (export is PropertyExport)
            {
            }
        }
    }

    private static void ExtractFromJsonAsset(string jsonPath, List<LocalizationEntry> sink)
    {
        var asset = UAsset.DeserializeJson(File.ReadAllText(jsonPath));
        foreach (var export in asset.Exports)
        {
            if (export is NormalExport ne)
            {
                for (int i = 0; i < ne.Data.Count; i++)
                {
                    var prop = ne.Data[i];
                    TraverseProperty(jsonPath, export, prop, prop.Name.ToString(), sink);
                }
            }
            else if (export is PropertyExport)
            {
            }
        }
    }

    private static void TraverseProperty(string assetPath, Export export, PropertyData prop, string path, List<LocalizationEntry> sink)
    {
        if (prop is TextPropertyData txt)
        {
            var entry = new LocalizationEntry
            {
                asset = assetPath,
                export = export.ObjectName.ToString(),
                property = path,
                value = txt.Value?.Value,
                source = txt.CultureInvariantString?.Value
            };
            sink.Add(entry);
            return;
        }

        if (prop is StructPropertyData s)
        {
            foreach (var inner in s.Value)
            {
                TraverseProperty(assetPath, export, inner, path + "." + inner.Name.ToString(), sink);
            }
            return;
        }

        if (prop is ArrayPropertyData a)
        {
            for (int i = 0; i < a.Value.Length; i++)
            {
                TraverseProperty(assetPath, export, a.Value[i], path + $"[{i}]", sink);
            }
            return;
        }

        if (prop is MapPropertyData m)
        {
            int idx = 0;
            foreach (var kv in m.Value)
            {
                var keyLabel = kv.Key.ToString();
                TraverseProperty(assetPath, export, kv.Value, path + $"{{{keyLabel}}}", sink);
                idx++;
            }
            return;
        }

    }

    private class LocalizationEntry
    {
        public string asset { get; set; } = string.Empty;
        public string export { get; set; } = string.Empty;
        public string property { get; set; } = string.Empty;
        public string? value { get; set; }
        public string? source { get; set; }
    }

    private class OutputEntry
    {
        public string asset { get; set; } = string.Empty;
        public string text { get; set; } = string.Empty;
        public string hash { get; set; } = string.Empty;
    }
}
