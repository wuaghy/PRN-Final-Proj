namespace RagChatbot.DataAccess.EntityModels
{
    public class Transaction
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public decimal Amount { get; set; }
        public string Type { get; set; } = "Premium"; // Ví dụ: "Premium", "WalletTopup"
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public decimal UsdVndRate { get; set; } // Snapshot tỷ giá tại thời điểm giao dịch

        // Navigation Properties
        public AppUser? User { get; set; }
    }
}
