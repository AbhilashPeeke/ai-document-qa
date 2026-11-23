namespace DocQaApi;

public static class VectorSearch
{
    public static List<ScoredChunk> Search(
        List<string> chunks, List<float[]> embeddings, float[] query, int top = 5)
    {
        return chunks
            .Select((text, i) => new ScoredChunk(i, Cosine(embeddings[i], query), text))
            .OrderByDescending(c => c.Score)
            .Take(top)
            .ToList();
    }

    private static double Cosine(float[] a, float[] b)
    {
        double dot = 0, normA = 0, normB = 0;
        int n = Math.Min(a.Length, b.Length);
        for (int i = 0; i < n; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }
        return dot / (Math.Sqrt(normA) * Math.Sqrt(normB) + 1e-9);
    }
}