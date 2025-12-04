namespace Kismet.Compiler.Syntax.Statements.Expressions.Binary;

public class EqualityOperator : EqualityExpression
{
    public override string ToString()
    {
        return $"({Left}) == ({Right})";
    }
}
