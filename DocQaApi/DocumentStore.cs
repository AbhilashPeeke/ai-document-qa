namespace DocQaApi;

public class DocumentStore
{
    public string? FileName { get; private set; }
    public string Text { get; private set; } = "";
    public List<string> Chunks { get; private set; } = new();
    public List<float[]>? Embeddings { get; private set; }

    public void Set(string fileName, string text, List<string> chunks, List<float[]>? embeddings = null)
    {
        FileName = fileName;
        Text = text;
        Chunks = chunks;
        Embeddings = embeddings;
    }
}