namespace KismetScript.Syntax.Statements.Expressions.Binary;

public class LessThanOperator : RelationalExpression
{
    public override string ToString()
    {
        return $"({Left}) < ({Right})";
    }
}
