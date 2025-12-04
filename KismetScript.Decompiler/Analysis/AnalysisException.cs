using System.Runtime.Serialization;

namespace KismetScript.Linker.Decompiler.Analysis
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
