using RagChatbot.Business.DTOs;

namespace RagChatbot.Business.Interfaces
{
    public interface ITextChunkingService
    {
        Task<List<string>> ChunkTextAsync(string text, ChunkConfig config);
    }
}
