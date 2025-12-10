using Docs_edits.Services;
using Docs_edits.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using System.Text;

[ApiController]
[Route("api/[controller]")]
public class AssistantController : ControllerBase
{
    private readonly AiService _service;
    private readonly IDistributedCache _cache;

    public AssistantController(AiService service, IDistributedCache cache)
    {
        _service = service;
        _cache = cache;
    }

    [HttpPost("ask")]
    public async Task<IActionResult> Ask([FromBody] PromptRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Prompt))
            return BadRequest(new { message = "Prompt cannot be empty." });

        try
        {
            var sessionId = string.IsNullOrWhiteSpace(request.SessionId) ? Guid.NewGuid().ToString("N") : request.SessionId;
            var cacheKey = $"chat:{sessionId}";

            List<ChatMessage> messages;
            var cached = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cached))
            {
                messages = JsonSerializer.Deserialize<List<ChatMessage>>(cached) ?? new List<ChatMessage>();
            }
            else
            {
                messages = new List<ChatMessage>
                {
                    new ChatMessage { Role = "system", Content = "You are an AI assistant.\r\n\r\nImportant: The app context below is only relevant if the user explicitly asks about the app. Otherwise, do not include any information about the app in your responses.\r\n\r\nApp context (ignore unless asked): This is a collaborative docs app that supports creating and editing rich text documents, real-time collaboration, PDF export, templates, comments, and tracked changes.\r\n\r\nFor all other queries, provide concise, actionable answers. When users ask to modify a document, propose precise edits (text replacements, insertions, deletions) with rationale. If context about the current document is missing, ask clarifying questions before suggesting changes." }
                };
            }

            messages.Add(new ChatMessage { Role = "user", Content = request.Prompt });

            var result = await _service.SendMessageAsync(messages);

            messages.Add(new ChatMessage { Role = "assistant", Content = result });

            await _cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(messages),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) }
            );

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("ask-stream")]
    public async Task AskStream([FromBody] PromptRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Prompt))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsync("Prompt cannot be empty.");
            return;
        }

        var sessionId = string.IsNullOrWhiteSpace(request.SessionId) ? Guid.NewGuid().ToString("N") : request.SessionId;
        var cacheKey = $"chat:{sessionId}";

        Response.Headers["Content-Type"] = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["Connection"] = "keep-alive";

        try
        {
            List<ChatMessage> messages;
            var cached = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cached))
            {
                messages = JsonSerializer.Deserialize<List<ChatMessage>>(cached) ?? new List<ChatMessage>();
            }
            else
            {
                messages = new List<ChatMessage>
                {
                    new ChatMessage { Role = "system", Content = "You are an AI assistant integrated into a collaborative docs app. The app supports: creating and editing rich text documents, real-time collaboration, PDF export, templates, comments, and tracked changes. Provide helpful answers tailored to document creation, formatting, and collaboration workflows. When users ask to modify a document, propose precise edits (text replacements, insertions, deletions) and rationale. Keep responses concise and actionable. If context about the current document is missing, ask clarifying questions before making changes." }
                };
            }

            messages.Add(new ChatMessage { Role = "user", Content = request.Prompt });

            var sb = new StringBuilder();
            await foreach (var chunk in _service.SendMessageStreamAsync(messages, HttpContext.RequestAborted))
            {
                sb.Append(chunk);
                await Response.WriteAsync($"data: {chunk}\n\n");
                await Response.Body.FlushAsync();
            }

            messages.Add(new ChatMessage { Role = "assistant", Content = sb.ToString() });

            await _cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(messages),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) }
            );
        }
        catch (Exception ex)
        {
            await Response.WriteAsync($"event: error\n");
            await Response.WriteAsync($"data: {ex.Message}\n\n");
        }
    }
}
