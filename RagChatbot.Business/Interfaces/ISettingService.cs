using RagChatbot.Business.DTOs;

namespace RagChatbot.Business.Interfaces
{
    public interface ISettingService
    {
        /// <summary>Đọc cấu hình chunking từ DB; key thiếu dùng mặc định (=hành vi cũ).</summary>
        Task<ChunkConfig> GetChunkConfigAsync();

        /// <summary>Ghi đè toàn bộ cấu hình chunking (upsert từng key).</summary>
        Task SaveChunkConfigAsync(ChunkConfig config);

        /// <summary>Lấy cấu hình giá (Tỷ giá USD, Giá Token In/Out).</summary>
        Task<PricingConfig> GetPricingConfigAsync();

        /// <summary>Lưu cấu hình giá.</summary>
        Task SavePricingConfigAsync(PricingConfig config);

        /// <summary>Lấy tỷ giá USD quy đổi hiện hành.</summary>
        Task<decimal> GetUsdRateAsync();
    }
}
