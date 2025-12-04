using Kismet.Compiler.Syntax.Statements;

namespace Kismet.Compiler.Syntax;

public class CompilationUnit : SyntaxNode
{
    public List<Declaration> Declarations { get; set; }

    public CompilationUnit()
    {
        Declarations = new List<Declaration>();
    }

    public CompilationUnit(List<Declaration> declarations)
    {
        Declarations = declarations;
    }
}
