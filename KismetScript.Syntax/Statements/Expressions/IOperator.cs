namespace KismetScript.Syntax.Statements.Expressions;

public interface IOperator
{
    int Precedence { get; }
}