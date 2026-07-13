using System.Globalization;
using Microsoft.EntityFrameworkCore;
using RagChatbot.Business.DTOs;
using RagChatbot.Business.Interfaces;
using RagChatbot.DataAccess.EntityModels;
using RagChatbot.DataAccess.Interfaces;

namespace RagChatbot.Business.Services
{
    public class SettingService : ISettingService
    {
        private readonly IAppSettingRepository _repo;

        public SettingService(IAppSettingRepository repo)
        {
            _repo = repo;
        }

        // Chunking config keys
        private const string WordsPerChunkEnabled = "Chunk.WordsPerChunk.Enabled";
        private const string WordsPerChunk = "Chunk.WordsPerChunk.Value";
        private const string CharsPerChunkEnabled = "Chunk.CharsPerChunk.Enabled";
        private const string CharsPerChunk = "Chunk.CharsPerChunk.Value";
        private const string WordsPerParagraphEnabled = "Chunk.WordsPerParagraph.Enabled";
        private const string WordsPerParagraph = "Chunk.WordsPerParagraph.Value";
        private const string ParagraphsPerChunkEnabled = "Chunk.ParagraphsPerChunk.Enabled";
        private const string ParagraphsPerChunk = "Chunk.ParagraphsPerChunk.Value";
        private const string TokensPerChunkEnabled = "Chunk.TokensPerChunk.Enabled";
        private const string TokensPerChunk = "Chunk.TokensPerChunk.Value";
        private const string Overlap = "Chunk.Overlap.Value";

        public async Task<ChunkConfig> GetChunkConfigAsync()
        {
            var map = await _repo.Query().ToDictionaryAsync(s => s.Key, s => s.Value);
            var cfg = new ChunkConfig(); // defaults = old behavior

            cfg.WordsPerChunkEnabled = GetBool(map, WordsPerChunkEnabled, cfg.WordsPerChunkEnabled);
            cfg.WordsPerChunk = GetInt(map, WordsPerChunk, cfg.WordsPerChunk);
            cfg.CharsPerChunkEnabled = GetBool(map, CharsPerChunkEnabled, cfg.CharsPerChunkEnabled);
            cfg.CharsPerChunk = GetInt(map, CharsPerChunk, cfg.CharsPerChunk);
            cfg.WordsPerParagraphEnabled = GetBool(map, WordsPerParagraphEnabled, cfg.WordsPerParagraphEnabled);
            cfg.WordsPerParagraph = GetInt(map, WordsPerParagraph, cfg.WordsPerParagraph);
            cfg.ParagraphsPerChunkEnabled = GetBool(map, ParagraphsPerChunkEnabled, cfg.ParagraphsPerChunkEnabled);
            cfg.ParagraphsPerChunk = GetInt(map, ParagraphsPerChunk, cfg.ParagraphsPerChunk);
            cfg.TokensPerChunkEnabled = GetBool(map, TokensPerChunkEnabled, cfg.TokensPerChunkEnabled);
            cfg.TokensPerChunk = GetInt(map, TokensPerChunk, cfg.TokensPerChunk);
            cfg.Overlap = GetInt(map, Overlap, cfg.Overlap);

            return cfg;
        }

        public async Task SaveChunkConfigAsync(ChunkConfig c)
        {
            var map = await _repo.Query().ToDictionaryAsync(s => s.Key, s => s);
            var toAdd = new List<AppSetting>();
            var inv = CultureInfo.InvariantCulture;

            Upsert(map, toAdd, WordsPerChunkEnabled, c.WordsPerChunkEnabled.ToString());
            Upsert(map, toAdd, WordsPerChunk, c.WordsPerChunk.ToString(inv));
            Upsert(map, toAdd, CharsPerChunkEnabled, c.CharsPerChunkEnabled.ToString());
            Upsert(map, toAdd, CharsPerChunk, c.CharsPerChunk.ToString(inv));
            Upsert(map, toAdd, WordsPerParagraphEnabled, c.WordsPerParagraphEnabled.ToString());
            Upsert(map, toAdd, WordsPerParagraph, c.WordsPerParagraph.ToString(inv));
            Upsert(map, toAdd, ParagraphsPerChunkEnabled, c.ParagraphsPerChunkEnabled.ToString());
            Upsert(map, toAdd, ParagraphsPerChunk, c.ParagraphsPerChunk.ToString(inv));
            Upsert(map, toAdd, TokensPerChunkEnabled, c.TokensPerChunkEnabled.ToString());
            Upsert(map, toAdd, TokensPerChunk, c.TokensPerChunk.ToString(inv));
            Upsert(map, toAdd, Overlap, c.Overlap.ToString(inv));

            if (toAdd.Count > 0) await _repo.AddRangeAsync(toAdd);
            await _repo.SaveChangesAsync();
        }

        public async Task<PricingConfig> GetPricingConfigAsync()
        {
            var map = await _repo.Query().ToDictionaryAsync(s => s.Key, s => s.Value);
            var cfg = new PricingConfig();

            var inv = CultureInfo.InvariantCulture;

            if (map.TryGetValue("UsdVndRate", out var usd) && decimal.TryParse(usd, NumberStyles.Any, inv, out var u)) cfg.UsdVndRate = u;
            if (map.TryGetValue("Price.TokenIn.Usd", out var ti) && decimal.TryParse(ti, NumberStyles.Any, inv, out var i)) cfg.TokenInCostPerMillion = i;
            if (map.TryGetValue("Price.TokenOut.Usd", out var to) && decimal.TryParse(to, NumberStyles.Any, inv, out var o)) cfg.TokenOutCostPerMillion = o;

            return cfg;
        }

        public async Task SavePricingConfigAsync(PricingConfig c)
        {
            var map = await _repo.Query().ToDictionaryAsync(s => s.Key, s => s);
            var toAdd = new List<AppSetting>();
            var inv = CultureInfo.InvariantCulture;

            Upsert(map, toAdd, "UsdVndRate", c.UsdVndRate.ToString(inv));
            Upsert(map, toAdd, "Price.TokenIn.Usd", c.TokenInCostPerMillion.ToString(inv));
            Upsert(map, toAdd, "Price.TokenOut.Usd", c.TokenOutCostPerMillion.ToString(inv));

            if (toAdd.Count > 0) await _repo.AddRangeAsync(toAdd);
            await _repo.SaveChangesAsync();
        }

        public async Task<decimal> GetUsdRateAsync()
        {
            var setting = await _repo.Query().FirstOrDefaultAsync(s => s.Key == "UsdVndRate");
            return decimal.TryParse(setting?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedRate) ? parsedRate : 25000m;
        }

        private void Upsert(Dictionary<string, AppSetting> map, List<AppSetting> toAdd, string key, string value)
        {
            if (map.TryGetValue(key, out var existing))
            {
                existing.Value = value;
                _repo.Update(existing);
            }
            else
            {
                toAdd.Add(new AppSetting { Key = key, Value = value });
            }
        }

        private static bool GetBool(Dictionary<string, string> map, string key, bool fallback)
            => map.TryGetValue(key, out var v) && bool.TryParse(v, out var b) ? b : fallback;

        private static int GetInt(Dictionary<string, string> map, string key, int fallback)
            => map.TryGetValue(key, out var v) && int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i) ? i : fallback;
    }
}
