using KismetCompiler.Library.Syntax;

namespace KismetCompiler.Library.Compiler.Exceptions;

public class CompilationError : Exception
{
    public CompilationError(SyntaxNode syntaxNode, string message)
        : base($"Compilation error at line {syntaxNode.SourceInfo.Line}: {message}")
    {

    }
}
