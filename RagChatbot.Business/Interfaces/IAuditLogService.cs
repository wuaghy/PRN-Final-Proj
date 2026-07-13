using System.Collections.Generic;
using System.Threading.Tasks;
using RagChatbot.DataAccess.EntityModels;

namespace RagChatbot.Business.Interfaces
{
    public interface IAuditLogService
    {
        Task LogAsync(int actorId, string action, string targetObjectId, string details);
        Task<IEnumerable<AuditLog>> GetAllLogsAsync();
    }
}
