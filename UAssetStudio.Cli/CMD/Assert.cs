namespace UAssetStudio.Cli.CMD
{
    internal static class Assert
    {
        public static void AreEqual(string expected, string actual)
        {
            if (string.Equals(expected, actual, StringComparison.Ordinal)) return;

            // Normalize line endings and split
            var eLines = expected.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            var aLines = actual.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

            int minLen = Math.Min(eLines.Length, aLines.Length);
            int diffLineIndex = -1;
            for (int i = 0; i < minLen; i++)
            {
                if (!string.Equals(eLines[i], aLines[i], StringComparison.Ordinal))
                {
                    diffLineIndex = i;
                    break;
                }
            }

            string eLine, aLine;
            if (diffLineIndex == -1)
            {
                // Lines equal up to minLen; difference is extra lines
                diffLineIndex = minLen; // first extra line position
                eLine = eLines.Length > diffLineIndex ? eLines[diffLineIndex] : "<EOF>";
                aLine = aLines.Length > diffLineIndex ? aLines[diffLineIndex] : "<EOF>";
            }
            else
            {
                eLine = eLines[diffLineIndex];
                aLine = aLines[diffLineIndex];
            }

            // Find first differing char within the differing line, for precision
            int charDiff = 0;
            int maxCharCheck = Math.Min(eLine.Length, aLine.Length);
            while (charDiff < maxCharCheck && eLine[charDiff] == aLine[charDiff]) charDiff++;

            var message =
                $"Assertion failed: values are not equal.\n" +
                $"First difference at line {diffLineIndex + 1}, char {charDiff + 1}.\n" +
                $"Expected: {eLine}\n" +
                $"Actual:   {aLine}";

            Console.WriteLine(message);
            throw new InvalidOperationException(message);
        }

        public static void AreEqual<T>(T expected, T actual)
        {
            if (expected is string se && actual is string sa)
            {
                AreEqual(se, sa);
                return;
            }

            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                var message = "Assertion failed: values are not equal.";
                Console.WriteLine(message);
                throw new InvalidOperationException(message);
            }
        }
    }
}
