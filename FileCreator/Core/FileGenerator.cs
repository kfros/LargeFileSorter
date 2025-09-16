using System.Text;
namespace FileCreator.Core;

public static class FileGenerator
{    
    public static void GenerateTestFile(string outputFile, long targetSizeBytes)
    {
        var firstWords = GenerateWords(1000, 3, 10, true); // First word with capital letter
        var otherWords = GenerateWords(1000, 2, 8, false);
        var rnd = new Random();
        long currentBytes = 0;

        using var writer = new StreamWriter(outputFile, false, Encoding.UTF8);

        while (currentBytes < targetSizeBytes)
        {
            int number = rnd.Next(1, 1000000);
            string firstWord = firstWords[rnd.Next(firstWords.Count)];

            int numOther = rnd.Next(0, 20);
            var otherParts = Enumerable.Range(0, numOther)
                .Select(_ => otherWords[rnd.Next(otherWords.Count)])
                .ToList();

            string stringPart = firstWord;
            if (otherParts.Count > 0)
                stringPart += " " + string.Join(" ", otherParts);

            string line = $"{number}. {stringPart}";
            int lineBytes = Encoding.UTF8.GetByteCount(line) + Encoding.UTF8.GetByteCount(Environment.NewLine);

            if (currentBytes + lineBytes > targetSizeBytes)
                break;

            writer.WriteLine(line);
            currentBytes += lineBytes;
        }
    }

    static List<string> GenerateWords(int count, int minLength, int maxLength, bool capitalizeFirstLetter)
    {
        var rnd = new Random();
        var words = new HashSet<string>();
        const string lowercaseLetters = "abcdefghijklmnopqrstuvwxyz";

        while (words.Count < count)
        {
            int length = rnd.Next(minLength, maxLength + 1);
            var chars = new char[length];

            // First character
            chars[0] = capitalizeFirstLetter
                ? char.ToUpper(lowercaseLetters[rnd.Next(lowercaseLetters.Length)])
                : lowercaseLetters[rnd.Next(lowercaseLetters.Length)];

            // Remaining characters
            for (int i = 1; i < length; i++)
            {
                chars[i] = lowercaseLetters[rnd.Next(lowercaseLetters.Length)];
            }

            words.Add(new string(chars));
        }
        return words.ToList();
    }
}