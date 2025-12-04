using KismetScript.Syntax.Statements.Expressions;

namespace KismetScript.Syntax.Statements.Declarations;

public class AttributeDeclaration : SyntaxNode
{
    public Identifier Identifier { get; set; } = null!;
    public List<Argument> Arguments { get; init; } = new();
}
