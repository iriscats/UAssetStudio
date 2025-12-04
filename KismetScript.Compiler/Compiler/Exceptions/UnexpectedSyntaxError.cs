using KismetScript.Syntax;

namespace KismetScript.Compiler.Compiler.Exceptions;

public class UnexpectedSyntaxError : CompilationError
{
    public UnexpectedSyntaxError(SyntaxNode syntaxNode)
        : base(syntaxNode, $"{syntaxNode.SourceInfo?.Line}:{syntaxNode.SourceInfo?.Column}: {syntaxNode} was unexpected at this time.")
    {
    }
}
