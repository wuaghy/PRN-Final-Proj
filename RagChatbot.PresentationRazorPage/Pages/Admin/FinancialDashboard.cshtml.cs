using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

using RagChatbot.DataAccess.Data;


namespace RagChatbot.PresentationRazorPage.Pages.Admin
{
    [Authorize(Roles = "Admin")] // Đảm bảo chỉ Admin mới vào được
    public class FinancialDashboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public FinancialDashboardModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Định nghĩa các chỉ số hiển thị ở View
        public decimal TotalRevenueVnd { get; set; }
        public long TotalInputTokens { get; set; }
        public long TotalOutputTokens { get; set; }
        public decimal TotalCostUsd { get; set; }
        public decimal TotalCostVnd { get; set; }
        public decimal NetProfitVnd { get; set; }
        public List<TransactionDto> RecentTransactions { get; set; } = new();

        public async Task OnGetAsync()
        {
            // 1. Tính tổng doanh thu từ VNPAY
            TotalRevenueVnd = await _context.Transactions.SumAsync(t => t.Amount);

            // 2. Thống kê lượng Token tiêu thụ từ ChatMessage
            var chatMetrics = await _context.ChatMessages
                .Where(m => m.Role == "assistant" && (m.TokenIn != null || m.TokenOut != null))
                .Select(m => new { m.TokenIn, m.TokenOut, m.UsdRate })
                .ToListAsync();

            TotalInputTokens = chatMetrics.Sum(m => (long)(m.TokenIn ?? 0));
            TotalOutputTokens = chatMetrics.Sum(m => (long)(m.TokenOut ?? 0));

            // Giá Gemini 1.5 Flash: Input=$0.075/1M tokens, Output=$0.30/1M tokens
            decimal inputCostRate = 0.075m / 1000000m;
            decimal outputCostRate = 0.30m / 1000000m;

            TotalCostUsd = (TotalInputTokens * inputCostRate) + (TotalOutputTokens * outputCostRate);

            // Tính toán chi phí quy đổi VND theo tỷ giá snapshot tại từng tin nhắn
            TotalCostVnd = chatMetrics.Sum(m =>
            {
                decimal msgCostUsd = ((m.TokenIn ?? 0) * inputCostRate) + ((m.TokenOut ?? 0) * outputCostRate);
                decimal msgUsdRate = m.UsdRate ?? 25000m; // Fallback nếu tỷ giá null
                return msgCostUsd * msgUsdRate;
            });

            // 3. Tính lợi nhuận ròng
            NetProfitVnd = TotalRevenueVnd - TotalCostVnd;

            // 4. Lấy danh sách 10 giao dịch gần nhất hiển thị lên bảng
            RecentTransactions = await _context.Transactions
                .Include(t => t.User)
                .OrderByDescending(t => t.CreatedAt)
                .Take(10)
                .Select(t => new TransactionDto
                {
                    UserEmail = t.User != null ? t.User.Email : "Ẩn danh",
                    Amount = t.Amount,
                    Rate = t.UsdVndRate,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync();
        }
    }

    public class TransactionDto
    {
        public string UserEmail { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal Rate { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}