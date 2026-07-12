using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RagChatbot.Business.Interfaces;
using RagChatbot.DataAccess.Data;
using RagChatbot.DataAccess.EntityModels;

namespace RagChatbot.Business.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly ApplicationDbContext _context;

        public TransactionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task ProcessPremiumUpgradeAsync(int userId, decimal amountVnd)
        {
            var user = await _context.AppUsers.FindAsync(userId);
            if (user != null)
            {
                // Cập nhật subscription của user thành Premium
                user.Subscription = AppUser.SubscriptionType.Premium;
                _context.AppUsers.Update(user);

                // Lấy tỷ giá USD quy đổi hiện tại từ AppSettings
                var usdRateSetting = await _context.AppSettings.FirstOrDefaultAsync(s => s.Key == "UsdVndRate");
                decimal currentUsdRate = decimal.TryParse(usdRateSetting?.Value, out var parsedRate) ? parsedRate : 25000m;

                // Tiến hành ghi log giao dịch
                var transaction = new Transaction
                {
                    UserId = userId,
                    Amount = amountVnd,
                    Type = "Premium",
                    CreatedAt = DateTime.UtcNow,
                    UsdVndRate = currentUsdRate
                };

                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();
            }
        }
    }
}
