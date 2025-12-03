namespace KismetCompiler.Library.Utilities;

public static class CompilerHelper
{
    public static bool IsPackageImportAttribute(KismetCompiler.Library.Syntax.Statements.Declarations.AttributeDeclaration attribute)
    {
        return attribute.Identifier.Text == "Import";
    }
}
