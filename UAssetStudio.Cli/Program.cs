using System.CommandLine;
using System.IO;
using System.Text;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using KismetKompiler.Decompiler;
using KismetKompiler.Library.Compiler;
using KismetKompiler.Library.Compiler.Exceptions;
using KismetKompiler.Library.Compiler.Processing;
using KismetKompiler.Library.Packaging;
using KismetKompiler.Library.Parser;
using Newtonsoft.Json;
using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.Kismet;
using UAssetAPI.UnrealTypes;
using UAssetAPI.Unversioned;

namespace UAssetStudio.Cli;

internal static class Program
{
    private static UAsset LoadAsset(EngineVersion ueVersion, string? mappings, string assetPath)
    {
        return mappings != null
            ? new UAsset(assetPath, ueVersion, new Usmap(mappings))
            : new UAsset(assetPath, ueVersion);
    }

    private static void DecompileToKms(UAsset asset, string outPath)
    {
        using var outWriter = new StreamWriter(outPath, false, Encoding.Unicode);
        var decompiler = new KismetDecompiler(outWriter);
        decompiler.Decompile(asset);
    }

    private static void PrintSyntaxError(int lineNumber, int startIndex, int endIndex, string[] lines)
    {
        if (lineNumber < 1 || lineNumber > lines.Length) throw new ArgumentOutOfRangeException(nameof(lineNumber));
        string line = lines[lineNumber - 1];
        int lineLength = line.Length;
        if (startIndex < 0 || endIndex < 0 || startIndex >= lineLength || endIndex >= lineLength) throw new ArgumentOutOfRangeException(nameof(startIndex));
        string highlightedLine = line.Substring(0, startIndex) + new string('^', endIndex - startIndex + 1) + line.Substring(endIndex + 1);
        var messagePrefix = $"Syntax error at line {lineNumber}:";
        Console.WriteLine($"{messagePrefix}{line}");
        Console.WriteLine(new string(' ', messagePrefix.Length) + highlightedLine);
    }

    private static CompiledScriptContext CompileKms(string inPath, EngineVersion engineVersion)
    {
        try
        {
            var parser = new KismetScriptASTParser();
            using var reader = new StreamReader(inPath, Encoding.Unicode);
            var compilationUnit = parser.Parse(reader);
            var typeResolver = new TypeResolver();
            typeResolver.ResolveTypes(compilationUnit);
            var compiler = new KismetScriptCompiler { EngineVersion = engineVersion };
            return compiler.CompileCompilationUnit(compilationUnit);
        }
        catch (ParseCanceledException ex)
        {
            if (ex.InnerException is InputMismatchException innerEx)
            {
                var lines = File.ReadAllLines(inPath);
                PrintSyntaxError(innerEx.OffendingToken.Line, innerEx.OffendingToken.Column, innerEx.OffendingToken.Column + innerEx.OffendingToken.Text.Length - 1, lines);
            }
            throw;
        }
    }

    private static void DumpOldAndNew(string fileName, UAsset asset, CompiledScriptContext script)
    {
        KismetSerializer.asset = asset;
        var oldJsons = asset.Exports
            .Where(x => x is FunctionExport)
            .Cast<FunctionExport>()
            .OrderBy(x => asset.GetClassExport()?.FuncMap.IndexOf(x.ObjectName))
            .Select(x => (x.ObjectName.ToString(), JsonConvert.SerializeObject(KismetSerializer.SerializeScript(x.ScriptBytecode), Formatting.Indented)));

        var newJsons = script.Classes
            .SelectMany(x => x.Functions)
            .Select(x => (x.Symbol.Name, JsonConvert.SerializeObject(KismetSerializer.SerializeScript(x.Bytecode.ToArray()), Formatting.Indented)));

        var oldJsonText = string.Join("\n", oldJsons);
        var newJsonText = string.Join("\n", newJsons);
        File.WriteAllText("old.json", oldJsonText);
        File.WriteAllText("new.json", newJsonText);
        if (oldJsonText != newJsonText) Console.WriteLine($"Verification failed: {fileName}");
    }

    private static void GenCfg(EngineVersion ueVersion, string? mappings, string assetPath, string outputDir)
    {
        var asset = LoadAsset(ueVersion, mappings, assetPath);
        var fileName = Path.GetFileName(assetPath);
        using var output = new StreamWriter(Path.Join(outputDir, Path.ChangeExtension(fileName, ".txt")));
        using var dotOutput = new StreamWriter(Path.Join(outputDir, Path.ChangeExtension(fileName, ".dot")));
        new KismetAnalyzer.SummaryGenerator(asset, output, dotOutput).Summarize();
    }

    public static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.Unicode;
        var root = new RootCommand("UAssetStudio CLI entrypoint (cfg/decompile/compile)");

        var ueVersion = new Option<EngineVersion>("--ue-version", () => EngineVersion.VER_UE4_27, "Unreal Engine version");
        var mappings = new Option<string?>("--mappings", description: "Path to .usmap for unversioned properties");

        // Default: CFG generator (backward-compatible with previous ToolMain)
        var assetInput = new Argument<string>("input", description: "Path to asset (.uasset/.umap)");
        var outputDir = new Option<string?>("--outdir", description: "Output directory for CFG/dot; default = asset directory");
        root.AddArgument(assetInput);
        root.AddOption(outputDir);
        root.AddOption(ueVersion);
        root.AddOption(mappings);
        root.SetHandler((EngineVersion ver, string? mapPath, string input, string? outdir) =>
        {
            var dir = outdir ?? Path.GetDirectoryName(input) ?? ".";
            GenCfg(ver, mapPath, input, dir);
        }, ueVersion, mappings, assetInput, outputDir);

        // Subcommand: decompile asset -> .kms
        var decompile = new Command("decompile", "Decompile .uasset/.umap to .kms")
        {
            new Argument<string>("asset", description: "Path to asset (.uasset/.umap)"),
            new Option<string?>("--outdir", description: "Output directory; default = asset directory")
        };
        decompile.AddOption(ueVersion);
        decompile.AddOption(mappings);
        decompile.SetHandler((EngineVersion ver, string? mapPath, string assetPath, string? outdir) =>
        {
            var asset = LoadAsset(ver, mapPath, assetPath);
            var dir = outdir ?? Path.GetDirectoryName(assetPath) ?? ".";
            var kmsPath = Path.Join(dir, Path.ChangeExtension(Path.GetFileName(assetPath), ".kms"));
            DecompileToKms(asset, kmsPath);
            Console.WriteLine($"Decompiled: {assetPath} -> {kmsPath}");
        }, ueVersion, mappings, decompile.Arguments[0] as Argument<string>, decompile.Options[0] as Option<string?>);
        root.Add(decompile);

        // Subcommand: compile .kms -> .uasset
        var compile = new Command("compile", "Compile .kms to .uasset")
        {
            new Argument<string>("script", description: "Path to input .kms"),
            new Option<string?>("--asset", description: "Original asset path (.uasset); defaults to script .uasset neighbor"),
            new Option<string?>("--outdir", description: "Output directory; default = script directory")
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
            var script = CompileKms(scriptPath, ver);
            var originalAssetPath = assetPath ?? Path.ChangeExtension(scriptPath, ".uasset");
            if (!File.Exists(originalAssetPath))
            {
                Console.WriteLine($"Error: Cannot find original asset file {originalAssetPath} for compilation");
                return;
            }
            var asset = LoadAsset(ver, mapPath, originalAssetPath);
            var newAsset = new UAssetLinker(asset)
                .LinkCompiledScript(script)
                .Build();
            var dir = outdir ?? Path.GetDirectoryName(scriptPath) ?? ".";
            var outFile = Path.Join(dir, Path.GetFileName(Path.ChangeExtension(scriptPath, ".compiled.uasset")));
            newAsset.Write(outFile);
            Console.WriteLine($"Compiled: {scriptPath} -> {outFile}");
        }, ueVersion, mappings, compile.Arguments[0] as Argument<string>, compile.Options[0] as Option<string?>, compile.Options[1] as Option<string?>);
        root.Add(compile);

        return root.Invoke(args);
    }
}
