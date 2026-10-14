using System.Text;
using Microsoft.AspNetCore.Mvc;
using UglyToad.PdfPig;

namespace DocQaApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly DocumentStore _store;

    public DocumentsController(DocumentStore store)
    {
        _store = store;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "Please choose a file." });

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        string text;

        using var memory = new MemoryStream();
        await file.CopyToAsync(memory);
        memory.Position = 0;

        if (extension == ".pdf")
        {
            var sb = new StringBuilder();
            using var pdf = PdfDocument.Open(memory);
            foreach (var page in pdf.GetPages())
            {
                sb.AppendLine(string.Join(" ", page.GetWords().Select(w => w.Text)));
            }
            text = sb.ToString();
        }
        else if (extension == ".txt")
        {
            text = Encoding.UTF8.GetString(memory.ToArray());
        }
        else
        {
            return BadRequest(new { error = "Only .pdf and .txt files are supported." });
        }

        if (string.IsNullOrWhiteSpace(text))
            return BadRequest(new { error = "No text found. A scanned PDF (images only) can't be read yet." });

        var chunks = TextChunker.Split(text);
        _store.Set(file.FileName, text, chunks);

        return Ok(new
        {
            fileName = file.FileName,
            characters = text.Length,
            chunkCount = chunks.Count,
            preview = text.Length > 300 ? text[..300] : text
        });
    }
    [HttpGet("search")]
    public IActionResult Search([FromQuery] string q)
    {
        if (_store.Chunks.Count == 0)
            return BadRequest(new { error = "Upload a document first." });

        var results = KeywordSearch.Search(_store.Chunks, q);

        return Ok(results.Select(r => new
        {
            chunkNumber = r.Index + 1,
            score = Math.Round(r.Score, 2),
            text = r.Text
        }));
    }
}