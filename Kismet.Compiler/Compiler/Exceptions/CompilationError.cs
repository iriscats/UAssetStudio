using KismetScript.Syntax;

namespace Kismet.Compiler.Compiler.Exceptions;

public class CompilationError : Exception
{
    public CompilationError(SyntaxNode syntaxNode, string message)
        : base($"Compilation error at line {syntaxNode.SourceInfo.Line}: {message}")
    {

    }
}
