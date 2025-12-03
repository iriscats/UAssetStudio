using KismetCompiler.Library.Syntax.Statements.Declarations;
using KismetCompiler.Library.Syntax.Statements.Expressions;

namespace KismetCompiler.Library.Syntax.Statements;

public abstract class Declaration : Statement
{
    public List<AttributeDeclaration> Attributes { get; set; } = new();

    public DeclarationType DeclarationType { get; }

    public Identifier Identifier { get; set; } = null!;

    protected Declaration(DeclarationType type)
    {
        DeclarationType = type;
    }

    protected Declaration(DeclarationType type, Identifier identifier)
    {
        DeclarationType = type;
        Identifier = identifier;
    }
}
