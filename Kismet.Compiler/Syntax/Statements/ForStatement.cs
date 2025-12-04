namespace Kismet.Compiler.Syntax.Statements;

public class ForStatement : Statement, IBlockStatement
{
    public Statement Initializer { get; set; } = null!;

    public Expression Condition { get; set; } = null!;

    public Expression AfterLoop { get; set; } = null!;

    public CompoundStatement Body { get; set; } = null!;

    IEnumerable<CompoundStatement> IBlockStatement.Blocks => new[] { Body }.Where(x => x != null);

    public ForStatement()
    {
    }

    public ForStatement(Statement initializer, Expression condition, Expression afterLoop, CompoundStatement body)
    {
        Initializer = initializer;
        Condition = condition;
        AfterLoop = afterLoop;
        Body = body;
    }
}
