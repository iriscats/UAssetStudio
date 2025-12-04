namespace Kismet.Compiler.Syntax.Statements.Expressions;

public interface IOperator
{
    int Precedence { get; }
}