using Docs_edits.DTOs;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace Docs_edits.Services
{
    public class AiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _chatUrl;

        public AiService(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClient = httpClientFactory.CreateClient();
            _chatUrl = config["AiServer:ChatUrl"] ?? "http://localhost:8000/chat";
        }

        public async Task<string> SendMessageAsync(List<ChatMessage> messages)
        {
            var userMessages = messages.Where(m => m.Role == "user").Select(m => m.Content);
            var prompt = string.Join("\n", userMessages);

            if (string.IsNullOrWhiteSpace(prompt))
            {
                throw new ArgumentException("No user message found in the conversation.");
            }

            var payload = new
            {
                prompt = prompt
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_chatUrl, content);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadAsStringAsync();
            
            try
            {
                using var doc = JsonDocument.Parse(result);
                var root = doc.RootElement;
                
                if (root.TryGetProperty("response", out var responseField))
                {
                    return responseField.GetString() ?? result.Trim();
                }
                if (root.TryGetProperty("message", out var messageField))
                {
                    return messageField.GetString() ?? result.Trim();
                }
                if (root.TryGetProperty("content", out var contentField))
                {
                    return contentField.GetString() ?? result.Trim();
                }
                
                return result.Trim();
            }
            catch
            {
                return result.Trim();
            }
        }

        private string? ParseStreamChunk(string data, string originalLine)
        {
            try
            {
                using var doc = JsonDocument.Parse(data);
                var root = doc.RootElement;
                
                if (root.TryGetProperty("content", out var contentField))
                {
                    return contentField.GetString();
                }
                else if (root.TryGetProperty("delta", out var deltaField) && deltaField.TryGetProperty("content", out var deltaContent))
                {
                    return deltaContent.GetString();
                }
                else if (root.TryGetProperty("message", out var messageField) && messageField.TryGetProperty("content", out var messageContent))
                {
                    return messageContent.GetString();
                }
            }
            catch
            {
                return !string.IsNullOrWhiteSpace(originalLine) ? originalLine : null;
            }
            
            return null;
        }

        public async IAsyncEnumerable<string> SendMessageStreamAsync(List<ChatMessage> messages, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var userMessages = messages.Where(m => m.Role == "user").Select(m => m.Content);
            var prompt = string.Join("\n", userMessages);

            if (string.IsNullOrWhiteSpace(prompt))
            {
                throw new ArgumentException("No user message found in the conversation.");
            }

            var payload = new
            {
                prompt = prompt
            };

            var json = JsonSerializer.Serialize(payload);
            using var request = new HttpRequestMessage(HttpMethod.Post, _chatUrl)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream, Encoding.UTF8);

            while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync();
                if (line == null) break;

                if (line.StartsWith("data: ", StringComparison.OrdinalIgnoreCase))
                {
                    var data = line.Substring(6).Trim();
                    if (data == "[DONE]")
                        yield break;

                    var chunk = ParseStreamChunk(data, line);
                    if (!string.IsNullOrEmpty(chunk))
                    {
                        yield return chunk;
                    }
                }
                else if (!string.IsNullOrWhiteSpace(line))
                {
                    yield return line;
                }
            }
        }
    }
}

