using System.Runtime.Serialization;

namespace KismetCompiler.Library.Decompiler.Analysis
{
    public class AnalysisException : Exception
    {
        public AnalysisException()
        {
        }

        public AnalysisException(string? message) : base(message)
        {
        }

        public AnalysisException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

    }
}
