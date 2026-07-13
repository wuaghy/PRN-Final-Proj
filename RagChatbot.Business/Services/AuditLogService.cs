using RagChatbot.Business.Interfaces;
using RagChatbot.Business.DTOs;
using RagChatbot.Business.Mappings;
using RagChatbot.DataAccess.EntityModels;
using RagChatbot.DataAccess.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RagChatbot.Business.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly IAuditLogRepository _auditLogRepository;

        public AuditLogService(IAuditLogRepository auditLogRepository)
        {
            _auditLogRepository = auditLogRepository;
        }

        public async Task LogAsync(int actorId, string action, string targetObjectId, string details)
        {
            var log = new AuditLog
            {
                ActorId = actorId,
                Action = action,
                TargetObjectId = targetObjectId,
                Details = details
            };

            await _auditLogRepository.AddAsync(log);
            await _auditLogRepository.SaveChangesAsync();
        }

        public async Task<IEnumerable<AuditLogDto>> GetAllLogsAsync()
        {
            var logs = await _auditLogRepository.Query()
                .Include(l => l.Actor)
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();

            return logs.Select(l => l.ToDto()!).ToList();
        }
    }
}
