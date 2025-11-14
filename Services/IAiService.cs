namespace Docs_edits.Services
{
    public interface IAiService
    {
        Task<string> AskAssistantAsync(string prompt);
    }
}
