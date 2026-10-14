using Microsoft.AspNetCore.Mvc;

namespace DocQaApi.Controllers;

public record ChatRequest(string Question);

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IAiService _ai;
    private readonly DocumentStore _store;

    public ChatController(IAiService ai, DocumentStore store)
    {
        _ai = ai;
        _store = store;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest(new { error = "Question is required." });

        if (_store.Chunks.Count == 0)
            return BadRequest(new { error = "Please upload a document first." });

        // 1. Retrieve: find the most relevant chunks
        var hits = KeywordSearch.Search(_store.Chunks, request.Question);

        if (hits.Count == 0)
        {
            return Ok(new
            {
                answer = "I couldn't find anything related to that in the document.",
                sources = Array.Empty<object>()
            });
        }

        // 2. Augment: build a prompt that contains only those chunks
        var context = string.Join("\n\n",
            hits.Select(h => $"[Chunk {h.Index + 1}]\n{h.Text}"));

        var prompt =
            "You are a helpful assistant that answers questions about a document.\n" +
            "Answer using ONLY the context below. " +
            "If the answer is not in the context, say: \"I couldn't find that in the document.\" " +
            "Do not make up information. Keep the answer clear and concise.\n\n" +
            "CONTEXT:\n" + context + "\n\n" +
            "QUESTION: " + request.Question;

        try
        {
            // 3. Generate: ask the AI
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