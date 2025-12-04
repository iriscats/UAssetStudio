using Kismet.Compiler.Syntax.Statements.Expressions.Identifiers;

namespace Kismet.Compiler.Syntax.Statements.Expressions;

public class NewExpression : Expression
{
    public TypeIdentifier TypeIdentifier { get; set; } = null!;
    public bool IsArray { get; set; }
    public int ArrayLength { get; set; }
    public List<Expression> Initializer { get; set; } = new();

    public NewExpression() : base(ValueKind.Unresolved)
    {
    }


    public override int GetDepth() => 1;
}
