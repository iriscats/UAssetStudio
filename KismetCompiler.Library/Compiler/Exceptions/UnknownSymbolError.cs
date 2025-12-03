using KismetCompiler.Library.Syntax.Statements.Expressions;

namespace KismetCompiler.Library.Compiler.Exceptions
{
    internal class UnknownSymbolError : Exception
    {
        private Identifier identifier;

        public UnknownSymbolError()
        {
        }

        public UnknownSymbolError(Identifier identifier)
        {
            this.identifier = identifier;
        }

        public UnknownSymbolError(string? message) : base(message)
        {
        }

        public UnknownSymbolError(string? message, Exception? innerException) : base(message, innerException)
        {
        }

    }
}
