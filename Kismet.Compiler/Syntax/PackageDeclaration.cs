using Kismet.Compiler.Syntax.Statements;
using Kismet.Compiler.Syntax.Statements.Expressions;

namespace Kismet.Compiler.Syntax;

public class PackageDeclaration : Declaration
{
    public PackageDeclaration() : base(DeclarationType.Package)
    {
    }

    public List<Declaration> Declarations { get; init; } = new();

    public override string ToString()
    {
        return $"from \"{Identifier.Text}\" import {{}}";
    }
}