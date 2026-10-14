namespace DocQaApi;

public class DocumentStore
{
    public string? FileName { get; private set; }
    public string Text { get; private set; } = "";
    public List<string> Chunks { get; private set; } = new();

    public void Set(string fileName, string text, List<string> chunks)
    {
        FileName = fileName;
        Text = text;
        Chunks = chunks;
    }
}