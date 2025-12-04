using Kismet.Compiler.Syntax.Statements.Expressions.Identifiers;

namespace Kismet.Compiler.Syntax.Statements.Expressions;

public class TypeofOperator : UnaryExpression, IOperator
{
    public int Precedence => 2;

    public TypeofOperator() : base(ValueKind.Type)
    {

    }

    public override int GetDepth() => 1;
}