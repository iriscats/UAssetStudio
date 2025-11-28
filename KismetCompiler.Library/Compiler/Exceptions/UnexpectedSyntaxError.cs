using KismetCompiler.Library.Syntax;

namespace KismetCompiler.Library.Compiler.Exceptions;

public class UnexpectedSyntaxError : CompilationError
{
    public UnexpectedSyntaxError(SyntaxNode syntaxNode)
        : base(syntaxNode, $"{syntaxNode.SourceInfo?.Line}:{syntaxNode.SourceInfo?.Column}: {syntaxNode} was unexpected at this time.")
    {
    }
}
