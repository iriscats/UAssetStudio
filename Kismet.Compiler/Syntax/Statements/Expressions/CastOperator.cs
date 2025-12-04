using Kismet.Compiler.Syntax.Statements.Expressions.Identifiers;

namespace Kismet.Compiler.Syntax.Statements.Expressions;

public class CastOperator : UnaryExpression, IOperator
{
    public TypeIdentifier TypeIdentifier { get; set; } = null!;

    public int Precedence => 2;

    public CastOperator() : base(ValueKind.Unresolved)
    {

    }

    public override int GetDepth() => 1;
}
