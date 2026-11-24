using Xunit;

namespace DocQaApi.Tests;

public class ChunkerTests
{
    [Fact]   // a test with no inputs
    public void Split_EmptyText_ReturnsNoChunks()
    {
        // Arrange
        var text = "";

        // Act
        var chunks = Chunker.Split(text);          // <-- placeholder: use your real class and method

        // Assert
        Assert.Empty(chunks);
    }

    [Theory]     // the same test run with several inputs
    [InlineData(500)]
    [InlineData(1000)]
    [InlineData(2500)]
    public void Split_LongText_NoChunkExceedsMaxSize(int length)
    {
        var text = new string('a', length);

        var chunks = Chunker.Split(text);          // <-- placeholder

        Assert.All(chunks, c => Assert.True(c.Length <= 1000));
    }
}