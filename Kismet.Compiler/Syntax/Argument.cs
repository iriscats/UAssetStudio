using Kismet.Compiler.Syntax.Statements;
using Kismet.Compiler.Syntax.Statements.Expressions;
using Kismet.Compiler.Syntax.Statements.Expressions.Identifiers;

namespace Kismet.Compiler.Syntax;

public class Argument : SyntaxNode
{
    public virtual Expression Expression { get; set; }

    public Argument()
    {
    }

    public Argument(Expression expression)
    {
        Expression = expression;
    }

    public override string ToString()
    {
        return $"{Expression}";
    }
}

public class OutArgument : Argument
{
    public Identifier Identifier { get; set; }

    public override Expression Expression
        => Identifier;

    public OutArgument()
    {
    }
}

public class OutDeclarationArgument : OutArgument
{
    public TypeIdentifier Type { get; set; }
}