using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RagChatbot.Business.Interfaces;
using RagChatbot.DataAccess.EntityModels;
using RagChatbot.DataAccess.Interfaces;

namespace RagChatbot.Business.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly IAppUserRepository _appUserRepository;
        private readonly IAppSettingRepository _appSettingRepository;
        private readonly ITransactionRepository _transactionRepository;

        public TransactionService(
            IAppUserRepository appUserRepository,
            IAppSettingRepository appSettingRepository,
            ITransactionRepository transactionRepository)
        {
            _appUserRepository = appUserRepository;
            _appSettingRepository = appSettingRepository;
            _transactionRepository = transactionRepository;
        }

        public async Task ProcessPremiumUpgradeAsync(int userId, decimal amountVnd)
        {
            var user = await _appUserRepository.GetByIdAsync(userId);
            if (user != null)
            {
                // Cập nhật subscription của user thành Premium
                user.Subscription = AppUser.SubscriptionType.Premium;
                _appUserRepository.Update(user);

                // Lấy tỷ giá USD quy đổi hiện tại từ AppSettings
                var usdRateSetting = await _appSettingRepository.Query().FirstOrDefaultAsync(s => s.Key == "UsdVndRate");
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

                await _transactionRepository.AddAsync(transaction);
                
                await _appUserRepository.SaveChangesAsync();
                await _transactionRepository.SaveChangesAsync();
            }
        }
    }
}
