using KismetCompiler.Library.Syntax.Statements.Expressions;

namespace KismetCompiler.Library.Syntax.Statements.Declarations;

public class AttributeDeclaration : SyntaxNode
{
    public Identifier Identifier { get; set; } = null!;
    public List<Argument> Arguments { get; init; } = new();
}
