using Docs_edits.DTOs;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace Docs_edits.Services
{
    public class OpenRouterService
    {
        private readonly HttpClient _httpClient;

        public OpenRouterService(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {config["OpenRouter:ApiKey"]}");
            
        }

        public async Task<string> SendMessageAsync(List<ChatMessage> messages)
        {
            var url = "https://openrouter.ai/api/v1/chat/completions";

            var payload = new
            {
                model = "tngtech/deepseek-r1t2-chimera:free",
                messages = messages.Select(m => new { role = m.Role, content = m.Content }).ToArray()
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(result);
            var message = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return message.Trim();
        }

        public async IAsyncEnumerable<string> SendMessageStreamAsync(List<ChatMessage> messages, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var url = "https://openrouter.ai/api/v1/chat/completions";

            var payload = new
            {
                model = "tngtech/deepseek-r1t2-chimera:free",
                stream = true,
                messages = messages.Select(m => new { role = m.Role, content = m.Content }).ToArray()
            };

            var json = JsonSerializer.Serialize(payload);
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
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

                if (line.StartsWith("data: "))
                {
                    var data = line.Substring(6).Trim();
                    if (data == "[DONE]")
                        yield break;

                    try
                    {
                        using var doc = JsonDocument.Parse(data);
                        var root = doc.RootElement;
                        var choices = root.GetProperty("choices");
                        if (choices.GetArrayLength() > 0)
                        {
                            var choice = choices[0];
                            if (choice.TryGetProperty("delta", out var delta) && delta.TryGetProperty("content", out var deltaContent))
                            {
                                var chunk = deltaContent.GetString();
                                
                            }
                            else if (choice.TryGetProperty("message", out var message) && message.TryGetProperty("content", out var messageContent))
                            {
                                var chunk = messageContent.GetString();
                                
                            }
                        }
                    }
                    catch
                    {
                        // ignore malformed chunks
                    }
                }
            }
        }
    }
}
