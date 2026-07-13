namespace RagChatbot.DataAccess.EntityModels
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public Guid SessionId { get; set; }
        public string Role { get; set; } = string.Empty; // "user" or "assistant"
        public string Content { get; set; } = string.Empty;
        public string? Citations { get; set; } // JSON array string
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // Các cột mới bổ sung
        public int? TokenIn { get; set; }
        public int? TokenOut { get; set; }
        public decimal? UsdRate { get; set; } // Snapshot tỷ giá tại thời điểm chat
        public decimal? TokenInCostPerMillion { get; set; } // Snapshot giá input/1M tokens
        public decimal? TokenOutCostPerMillion { get; set; } // Snapshot giá output/1M tokens

        // Navigation Properties
        public ChatSession? Session { get; set; }
    }
}
