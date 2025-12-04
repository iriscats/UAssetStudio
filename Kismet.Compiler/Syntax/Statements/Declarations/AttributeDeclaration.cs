using Kismet.Compiler.Syntax.Statements.Expressions;

namespace Kismet.Compiler.Syntax.Statements.Declarations;

public class AttributeDeclaration : SyntaxNode
{
    public Identifier Identifier { get; set; } = null!;
    public List<Argument> Arguments { get; init; } = new();
}
