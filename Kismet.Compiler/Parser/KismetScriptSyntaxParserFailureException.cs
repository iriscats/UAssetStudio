namespace Kismet.Compiler.Parser;

internal class KismetScriptSyntaxParserFailureException : Exception
{
    public KismetScriptSyntaxParserFailureException()
    {
    }

    public KismetScriptSyntaxParserFailureException(string? message) : base(message)
    {
    }

    public KismetScriptSyntaxParserFailureException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

}
