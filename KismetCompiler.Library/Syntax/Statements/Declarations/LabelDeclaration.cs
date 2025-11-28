using KismetCompiler.Library.Syntax.Statements.Expressions;

namespace KismetCompiler.Library.Syntax.Statements.Declarations;

public class LabelDeclaration : Declaration
{
    public LabelDeclaration() : base(DeclarationType.Label)
    {
    }

    public LabelDeclaration(Identifier identifier) : base(DeclarationType.Label, identifier)
    {

    }

    public override string ToString()
    {
        return $"{Identifier}:";
    }
}
