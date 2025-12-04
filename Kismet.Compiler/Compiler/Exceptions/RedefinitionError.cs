using Kismet.Compiler.Compiler.Context;
using KismetScript.Syntax;

namespace Kismet.Compiler.Compiler.Exceptions;

public class RedefinitionError : CompilationError
{
    public RedefinitionError(SyntaxNode syntaxNode)
        : base(syntaxNode, $"{syntaxNode.SourceInfo?.Line}:{syntaxNode.SourceInfo?.Column}: {syntaxNode} redefinition.")
    {

    }

    public RedefinitionError(Symbol symbol)
        : this(symbol.Declaration) { }

}
