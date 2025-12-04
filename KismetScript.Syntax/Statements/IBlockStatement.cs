namespace KismetScript.Syntax.Statements;

public interface IBlockStatement
{
    IEnumerable<CompoundStatement> Blocks { get; }
}
