using Microsoft.AspNetCore.Mvc;

namespace DocQaApi.Controllers;

public record ChatRequest(string Question);

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IAiService _ai;
    private readonly IEmbeddingService _embedder;
    private readonly DocumentStore _store;

    public ChatController(IAiService ai, IEmbeddingService embedder, DocumentStore store)
    {
        _ai = ai;
        _embedder = embedder;
        _store = store;
    }

    private async Task<List<ScoredChunk>> RetrieveAsync(string question)
    {
        // Preferred: search by meaning
        if (_store.Embeddings != null && _store.Embeddings.Count == _store.Chunks.Count)
        {
            try
            {
                var queryVector = await _embedder.EmbedQueryAsync(question);
                return VectorSearch.Search(_store.Chunks, _store.Embeddings, queryVector, 5);
            }
            catch
            {
                // fall through to keyword search
            }
        }

        // Fallback: search by words
        return KeywordSearch.Search(_store.Chunks, question, 6);
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest(new { error = "Question is required." });

        if (_store.Chunks.Count == 0)
            return BadRequest(new { error = "Please upload a document first." });

        var hits = await RetrieveAsync(request.Question);

        if (hits.Count == 0)
        {
            return Ok(new
            {
                answer = "I couldn't find anything related to that in the document.",
                sources = Array.Empty<object>()
            });
        }

        var context = string.Join("\n\n",
            hits.Select(h => $"[Chunk {h.Index + 1}]\n{h.Text}"));

        var prompt =
            "You are a helpful assistant that answers questions about a document.\n" +
            "Answer using ONLY the context below. " +
            "If the answer is not in the context, say: \"I couldn't find that in the document.\" " +
            "Do not make up information. Keep the answer clear and concise. " +
            "If the question is just a word or a topic, summarize what the context says about it.\n\n" +
            "CONTEXT:\n" + context + "\n\n" +
            "QUESTION: " + request.Question;

        try
        {
            var answer = await _ai.AskAsync(prompt);

            return Ok(new
            {
                answer,
                sources = hits.Select(h => new
                {
                    chunkNumber = h.Index + 1,
                    text = h.Text.Length > 250 ? h.Text[..250] + "..." : h.Text
                })
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}