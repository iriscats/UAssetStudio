using System.CommandLine;
using System.IO;
using UAssetAPI.UnrealTypes;

namespace UAssetStudio.Cli.CMD
{
    internal static class CfgCommandBuilder
    {
        internal static Command Create(Option<EngineVersion> ueVersion, Option<string?> mappings)
        {
            var inputArg = new Argument<string>("input", description: "Path to asset (.uasset/.umap)");
            var outdirOpt = new Option<string?>("--outdir", description: "Output directory for CFG/dot; default = asset directory");

            var cfg = new Command("cfg", "Generate CFG and DOT for an asset")
            {
                inputArg,
                outdirOpt
            };

            cfg.AddOption(ueVersion);
            cfg.AddOption(mappings);

            cfg.SetHandler((EngineVersion ver, string? mapPath, string input, string? outdir) =>
            {
                var dir = outdir ?? Path.GetDirectoryName(input) ?? ".";
                CliHelpers.GenCfg(ver, mapPath, input, dir);
            }, ueVersion, mappings, inputArg, outdirOpt);

            return cfg;
        }
    }
}
