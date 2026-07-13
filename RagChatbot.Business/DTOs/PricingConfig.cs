namespace RagChatbot.Business.DTOs
{
    public class PricingConfig
    {
        public decimal UsdVndRate { get; set; } = 25000m;
        public decimal TokenInCostPerMillion { get; set; } = 0.075m;
        public decimal TokenOutCostPerMillion { get; set; } = 0.30m;
    }
}
