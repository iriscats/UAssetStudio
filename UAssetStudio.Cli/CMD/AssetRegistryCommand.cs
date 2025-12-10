using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using AssetRegistry.Serializer;

namespace UAssetStudio.Cli.CMD
{
    internal static class AssetRegistryCommandBuilder
    {
        internal static Command Create()
        {
            var defaultPath = "/Users/bytedance/Project/UAssetStudio/script/AssetRegistry.bin";
            var pathOpt = new Option<string>("--path", () => defaultPath, "Path to AssetRegistry.bin");

            var cmd = new Command("asset-registry", "Parse AssetRegistry.bin and print summary")
            {
                pathOpt
            };

            cmd.SetHandler((string path) =>
            {
                if (!File.Exists(path))
                {
                    Console.WriteLine($"File not found: {path}");
                    return;
                }

                var bytes = File.ReadAllBytes(path);
                var reg = new AssetRegistry.Serializer.AssetRegistry();
                try
                {
                    reg.Read(bytes);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Error] Failed to parse asset registry: {ex.GetType().Name}: {ex.Message}");
                    return;
                }

                Console.WriteLine($"Header: {BitConverter.ToString(reg.Header)}");
                Console.WriteLine($"Unknown: {reg.Unknown}");
                Console.WriteLine($"StringTable size: {reg.keyValuePairs.Count}");
                Console.WriteLine($"Entries: {reg.fAssetDatas.Count}");

                var take = Math.Min(5, reg.fAssetDatas.Count);
                for (int idx = 0; idx < take; idx++)
                {
                    var item = reg.fAssetDatas[idx];
                    Console.WriteLine($"[{idx}] ObjectPath={item.ObjectPath} PackageName={item.PackageName} AssetClass={item.AssetClass} Tags={item.TagAndValue.Count} Chunks={item.ChunkIDs.Count}");
                }

                Console.WriteLine("Parse OK");
            }, pathOpt);

            return cmd;
        }
    }
}
