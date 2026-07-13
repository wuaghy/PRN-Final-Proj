namespace RagChatbot.Business.DTOs
{
    public class ChatMessageDto
    {
        public int Id { get; set; }
        public Guid SessionId { get; set; }
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? Citations { get; set; }
        public DateTime Timestamp { get; set; }

        public int? TokenIn { get; set; }
        public int? TokenOut { get; set; }
        public decimal? UsdRate { get; set; }
        public decimal? TokenInCostPerMillion { get; set; }
        public decimal? TokenOutCostPerMillion { get; set; }
    }

    public class CreateChatMessageDto
    {
        public Guid SessionId { get; set; }
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? Citations { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public int? TokenIn { get; set; }
        public int? TokenOut { get; set; }
        public decimal? UsdRate { get; set; }
        public decimal? TokenInCostPerMillion { get; set; }
        public decimal? TokenOutCostPerMillion { get; set; }
    }
}
