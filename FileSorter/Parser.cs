using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSorter
{
    public static class Parser
    {
        // We expect lines in the format: <number>. <text>
        public static bool TryParseLine(string line, out string text, out int number)
        {
            text = "";
            number = 0;
            if (string.IsNullOrWhiteSpace(line)) return false;

            int dot = line.IndexOf('.');
            if (dot <= 0) return false;

            ReadOnlySpan<char> head = line.AsSpan(0, dot).Trim();
            for (int i = 0; i < head.Length; i++)
                if (!char.IsDigit(head[i])) return false;

            if (!int.TryParse(head, NumberStyles.None, CultureInfo.InvariantCulture, out number))
                return false;

            ReadOnlySpan<char> tail = line.AsSpan(dot + 1).TrimStart();
            if (tail.Length == 0) return false;

            text = tail.ToString().Trim();
            return text.Length > 0;
        }

        public static bool TryReadParsed(StreamReader sr, out string line, out string text, out int number)
        {
            while ((line = sr.ReadLine()) != null)
            {
                if (TryParseLine(line, out text, out number))
                    return true;

                // Non-conforming line: push to the end using sentinel key
                text = "\uFFFF" + line;
                number = int.MaxValue;
                return true;
            }
            text = "";
            number = 0;
            return false;
        }
    }
}