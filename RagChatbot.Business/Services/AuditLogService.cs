using RagChatbot.Business.Interfaces;
using RagChatbot.DataAccess.EntityModels;
using RagChatbot.DataAccess.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RagChatbot.Business.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly ApplicationDbContext _context;

        public AuditLogService(ApplicationDbContext context)
        {
            _context = context;
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

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<AuditLog>> GetAllLogsAsync()
        {
            return await _context.AuditLogs
                .Include(l => l.Actor)
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();
        }
    }
}
