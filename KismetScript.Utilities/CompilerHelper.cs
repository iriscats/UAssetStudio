namespace KismetScript.Utilities;

public static class CompilerHelper
{
    public static bool IsPackageImportAttribute(KismetScript.Syntax.Statements.Declarations.AttributeDeclaration attribute)
    {
        return attribute.Identifier.Text == "Import";
    }
}
