using KismetCompiler.Library.Syntax.Statements;
using KismetCompiler.Library.Syntax.Statements.Expressions;

namespace KismetCompiler.Library.Syntax;

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