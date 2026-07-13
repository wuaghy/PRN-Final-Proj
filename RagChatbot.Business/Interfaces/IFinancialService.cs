using System.Threading.Tasks;
using RagChatbot.Business.DTOs;

namespace RagChatbot.Business.Interfaces
{
    public interface IFinancialService
    {
        Task<FinancialDashboardDto> GetDashboardDataAsync();
        Task<System.Collections.Generic.IEnumerable<AppUserDto>> GetTopTokenConsumersAsync(int topCount = 10);
    }
}
