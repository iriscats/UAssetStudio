namespace UAssetStudio.Cli.CMD
{
    internal static class Assert
    {
        public static void AreEqual<T>(T expected, T actual)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new InvalidOperationException("Assertion failed: values are not equal.");
            }
        }
    }
}
