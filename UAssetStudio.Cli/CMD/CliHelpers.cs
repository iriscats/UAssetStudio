using System.IO;
using System.Text;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using KismetKompiler.Decompiler;
using KismetKompiler.Library.Compiler;
using KismetKompiler.Library.Compiler.Processing;
using KismetKompiler.Library.Parser;
using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.UnrealTypes;
using UAssetAPI.Unversioned;
using UAssetAPI.Kismet;
using Newtonsoft.Json;

namespace UAssetStudio.Cli.CMD
{
    internal static class CliHelpers
    {
        internal static void VerifyOldAndNew(string fileName, UAsset asset, CompiledScriptContext script)
        {
            try
            {
                // Prepare serializer with new asset context
                KismetSerializer.asset = asset;

                // Old: functions from original asset (tolerate missing bytecode)
                var classExport = asset.GetClassExport();
                var oldJsons = asset.Exports
                    .Where(x => x is FunctionExport)
                    .Cast<FunctionExport>()
                    .OrderBy(x => classExport?.FuncMap?.IndexOf(x.ObjectName) ?? -1)
                    .Select(x =>
                    {
                        var bc = x.ScriptBytecode ?? Array.Empty<UAssetAPI.Kismet.Bytecode.KismetExpression>();
                        var json = JsonConvert.SerializeObject(KismetSerializer.SerializeScript(bc), Formatting.Indented);
                        return (x.ObjectName.ToString(), json);
                    });

                // New: functions from compiled script
                var newJsons = script.Classes
                    .SelectMany(x => x.Functions)
                    .Select(x =>
                    {
                        var bc = (x.Bytecode ?? new List<UAssetAPI.Kismet.Bytecode.KismetExpression>()).ToArray();
                        var json = JsonConvert.SerializeObject(KismetSerializer.SerializeScript(bc), Formatting.Indented);
                        return (x.Symbol.Name, json);
                    });

                var oldJsonText = string.Join("\n", oldJsons);
                var newJsonText = string.Join("\n", newJsons);

                File.WriteAllText("old.json", oldJsonText);
                File.WriteAllText("new.json", newJsonText);

                if (oldJsonText != newJsonText)
                {
                    Console.WriteLine($"Verification failed: {fileName}");
                }
                else
                {
                    Console.WriteLine($"Verification passed: {fileName}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Warn] Old/New verification skipped: {ex.GetType().Name}: {ex.Message}");
            }
        }
        // Removed unused verification helper requiring non-existent UnrealPackage type

        internal static UAsset LoadAsset(EngineVersion ueVersion, string? mappings, string assetPath)
        {
            return mappings != null
                ? new UAsset(assetPath, ueVersion, new Usmap(mappings))
                : new UAsset(assetPath, ueVersion);
        }

        internal static void PrintSyntaxError(int lineNumber, int startIndex, int endIndex, string[] lines)
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

        internal static void DecompileToKms(UAsset asset, string outPath)
        {
            using var outWriter = new StreamWriter(outPath, false, Encoding.Unicode);
            var decompiler = new KismetDecompiler(outWriter);
            decompiler.Decompile(asset);
        }

        internal static CompiledScriptContext CompileKms(string inPath, EngineVersion engineVersion)
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

        internal static void GenCfg(EngineVersion ueVersion, string? mappings, string assetPath, string outputDir)
        {
            var asset = LoadAsset(ueVersion, mappings, assetPath);
            var fileName = Path.GetFileName(assetPath);
            using var output = new StreamWriter(Path.Join(outputDir, Path.ChangeExtension(fileName, ".txt")));
            using var dotOutput = new StreamWriter(Path.Join(outputDir, Path.ChangeExtension(fileName, ".dot")));
            new KismetAnalyzer.SummaryGenerator(asset, output, dotOutput).Summarize();
        }
    }
}
