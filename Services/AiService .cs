using System.Linq;
using System.Threading.Tasks;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Responses;

namespace Docs_edits.Services
{
    public class AiService : IAiService
    {
        private readonly ChatClient _chatClient;

        public AiService(ChatClient chatClient)
        {
            _chatClient = chatClient;
        }

        public async Task<string> AskAssistantAsync(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                return "Prompt cannot be empty";

            var completion = await _chatClient.CompleteChatAsync(prompt);

            var message = completion.Value;
            return message.Content.FirstOrDefault()?.Text ?? string.Empty;
        }
    }
}