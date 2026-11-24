using DocQaApi;
using Xunit;

namespace DocQaApi.Tests;

public class VectorSearchTests
{
    private static readonly List<string> Chunks = new() { "a", "b", "c", "d", "e", "f" };

    private static List<float[]> SixEmbeddings() => new()
    {
        new float[] { 1, 0 },
        new float[] { 0, 1 },
        new float[] { 1, 1 },
        new float[] { -1, 0 },
        new float[] { 0.9f, 0.1f },
        new float[] { 0.1f, 0.9f },
    };

    [Fact]
    public void Search_ReturnsOnlyTopN()
    {
        var result = VectorSearch.Search(Chunks, SixEmbeddings(), new float[] { 1, 0 }, top: 3);

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void Search_FewerChunksThanTop_ReturnsAllChunks()
    {
        var chunks = new List<string> { "a", "b" };
        var embeddings = new List<float[]> { new float[] { 1, 0 }, new float[] { 0, 1 } };

        var result = VectorSearch.Search(chunks, embeddings, new float[] { 1, 0 }, top: 5);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Search_ResultsAreSortedByScoreDescending()
    {
        var result = VectorSearch.Search(Chunks, SixEmbeddings(), new float[] { 1, 0 }, top: 6);

        for (int i = 1; i < result.Count; i++)
            Assert.True(result[i - 1].Score >= result[i].Score);
    }

    [Fact]
    public void Search_IdenticalVector_ScoresAboutOne()
    {
        var result = VectorSearch.Search(
            new List<string> { "a" },
            new List<float[]> { new float[] { 1, 0 } },
            new float[] { 1, 0 });

        Assert.Equal(1.0, result[0].Score, 5);
    }

    [Fact]
    public void Search_PerpendicularVector_ScoresAboutZero()
    {
        var result = VectorSearch.Search(
            new List<string> { "a" },
            new List<float[]> { new float[] { 1, 0 } },
            new float[] { 0, 1 });

        Assert.Equal(0.0, result[0].Score, 5);
    }

    [Fact]
    public void Search_OppositeVector_ScoresAboutMinusOne()
    {
        var result = VectorSearch.Search(
            new List<string> { "a" },
            new List<float[]> { new float[] { 1, 0 } },
            new float[] { -1, 0 });

        Assert.Equal(-1.0, result[0].Score, 5);
    }

    [Fact]
    public void Search_BestMatchScoresHigherThanWorstMatch()
    {
        var result = VectorSearch.Search(Chunks, SixEmbeddings(), new float[] { 1, 0 }, top: 6);

        Assert.True(result.First().Score > result.Last().Score);
    }
}

public class DocumentStoreTests
{
    [Fact]
    public void NewStore_IsEmpty()
    {
        var store = new DocumentStore();

        Assert.Null(store.FileName);
        Assert.Equal("", store.Text);
        Assert.Empty(store.Chunks);
        Assert.Null(store.Embeddings);
    }

    [Fact]
    public void Set_StoresAllValues()
    {
        var store = new DocumentStore();
        var chunks = new List<string> { "one", "two" };
        var embeddings = new List<float[]> { new float[] { 1 }, new float[] { 2 } };

        store.Set("notes.txt", "full text", chunks, embeddings);

        Assert.Equal("notes.txt", store.FileName);
        Assert.Equal("full text", store.Text);
        Assert.Equal(2, store.Chunks.Count);
        Assert.Equal(2, store.Embeddings!.Count);
    }

    [Fact]
    public void Set_SecondCall_ReplacesThePreviousDocument()
    {
        var store = new DocumentStore();
        store.Set("old.txt", "old", new List<string> { "x" });

        store.Set("new.txt", "new", new List<string> { "y", "z" });

        Assert.Equal("new.txt", store.FileName);
        Assert.Equal(2, store.Chunks.Count);
        Assert.Null(store.Embeddings);
    }
}