using System;

namespace RagChatbot.Business.DTOs
{
    public class AuditLogDto
    {
        public int Id { get; set; }
        public int ActorId { get; set; }
        public DateTime Timestamp { get; set; }
        public string Action { get; set; } = string.Empty;
        public string TargetObjectId { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;

        public AppUserDto? Actor { get; set; }
    }
}
