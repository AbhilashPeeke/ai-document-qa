using System.Text.RegularExpressions;

namespace DocQaApi;

public static class TextChunker
{
    public static List<string> Split(string text, int size = 1000, int overlap = 200)
    {
        var chunks = new List<string>();
        text = Regex.Replace(text, @"\s+", " ").Trim();

        int start = 0;
        while (start < text.Length)
        {
            int end = Math.Min(start + size, text.Length);

            // Try to cut at a space so words are not split in half
            if (end < text.Length)
            {
                int lastSpace = text.LastIndexOf(' ', end - 1, Math.Min(200, end - start));
                if (lastSpace > start) end = lastSpace;
            }

            chunks.Add(text[start..end].Trim());

            if (end >= text.Length) break;
            start = Math.Max(end - overlap, start + 1);
        }

        return chunks;
    }
}