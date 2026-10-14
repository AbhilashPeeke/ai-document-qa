using System.Text.RegularExpressions;

namespace DocQaApi;

public record ScoredChunk(int Index, double Score, string Text);

public static class KeywordSearch
{
    private static readonly HashSet<string> StopWords = new()
    {
        "the","a","an","is","are","was","were","of","to","in","on","for","and","or",
        "what","how","do","does","did","i","you","me","my","your","it","this","that",
        "with","about","tell","explain","give","can","will","be","as","at","by","from"
    };

    private static List<string> Tokenize(string text) =>
        Regex.Matches(text.ToLowerInvariant(), @"[a-z0-9#+.]+")
             .Select(m => m.Value.Trim('.'))
             .Where(t => t.Length > 1 && !StopWords.Contains(t))
             .ToList();

    public static List<ScoredChunk> Search(List<string> chunks, string query, int top = 4)
    {
        var terms = Tokenize(query).Distinct().ToList();
        if (terms.Count == 0 || chunks.Count == 0) return new();

        var chunkTokens = chunks.Select(Tokenize).ToList();
        int n = chunks.Count;

        // Rare words get a higher weight (idf)
        var idf = terms.ToDictionary(
            t => t,
            t =>
            {
                int df = chunkTokens.Count(tokens => tokens.Contains(t));
                return Math.Log(1 + (double)n / (1 + df));
            });

        var results = new List<ScoredChunk>();
        for (int i = 0; i < n; i++)
        {
            double score = 0;
            foreach (var term in terms)
            {
                int tf = chunkTokens[i].Count(x => x == term);
                if (tf > 0) score += (1 + Math.Log(tf)) * idf[term];
            }
            if (score > 0) results.Add(new ScoredChunk(i, score, chunks[i]));
        }

        return results.OrderByDescending(r => r.Score).Take(top).ToList();
    }
}