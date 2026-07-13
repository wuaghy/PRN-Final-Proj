using System.Collections.Generic;
using System.Threading.Tasks;
using RagChatbot.Business.DTOs;

namespace RagChatbot.Business.Interfaces
{
    public interface IAuditLogService
    {
        Task LogAsync(int actorId, string action, string targetObjectId, string details);
        Task<IEnumerable<AuditLogDto>> GetAllLogsAsync();
    }
}
