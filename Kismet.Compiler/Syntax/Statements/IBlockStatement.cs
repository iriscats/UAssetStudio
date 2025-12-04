namespace Kismet.Compiler.Syntax.Statements;

public interface IBlockStatement
{
    IEnumerable<CompoundStatement> Blocks { get; }
}
