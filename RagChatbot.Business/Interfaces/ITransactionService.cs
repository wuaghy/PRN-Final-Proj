using System.Threading.Tasks;

namespace RagChatbot.Business.Interfaces
{
    public interface ITransactionService
    {
        Task ProcessPremiumUpgradeAsync(int userId, decimal amountVnd);
    }
}
