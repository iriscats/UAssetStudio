using System.IO;
using System.Text;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using KismetKompiler.Decompiler;
using KismetKompiler.Library.Compiler;
using KismetKompiler.Library.Compiler.Processing;
using KismetKompiler.Library.Parser;
using UAssetAPI;
using UAssetAPI.UnrealTypes;
using UAssetAPI.Unversioned;

namespace UAssetStudio.Cli.CMD
{
    internal static class CliHelpers
    {
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

