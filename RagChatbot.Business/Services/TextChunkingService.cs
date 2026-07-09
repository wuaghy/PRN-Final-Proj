#pragma warning disable SKEXP0050
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Text;
using RagChatbot.Business.DTOs;
using RagChatbot.Business.Interfaces;

namespace RagChatbot.Business.Services
{
    public class TextChunkingService : ITextChunkingService
    {
        private readonly ILogger<TextChunkingService> _logger;

        public TextChunkingService(ILogger<TextChunkingService> logger)
        {
            _logger = logger;
        }

        public Task<List<string>> ChunkTextAsync(string rawText, ChunkConfig config)
        {
            if (string.IsNullOrWhiteSpace(rawText)) return Task.FromResult(new List<string>());

            // Clean repeating layout noise and remove global structural artifacts
            string cleanedText = Regex.Replace(rawText, @"NGUYÊN LÝ KẾ TOÁN\s*\.\s*", " ");

            // Capture and collapse all formatting spaces or layout newlines around numeric periods
            while (Regex.IsMatch(cleanedText, @"(\d+)\s*\.\s*(\d+)"))
            {
                cleanedText = Regex.Replace(cleanedText, @"(\d+)\s*\.\s*(\d+)", "$1ALPHANUMERICDOTMASK$2");
            }

            // Isolate markdown bold tag boundaries from word tokens
            cleanedText = cleanedText.Replace("**", " ** ");

            var chunks = config.IsTokenOnly
                ? ChunkTokensOnly(cleanedText, config.TokensPerChunk, config.Overlap)
                : ChunkWithLimits(cleanedText, config);

            // Reassemble ordered collection and decode hidden mask values
            var finalizedChunks = new List<string>();
            foreach (var chunk in chunks)
            {
                string restoredText = chunk.Replace("ALPHANUMERICDOTMASK", ".").Trim();

                // Clean up accidental trailing fragments if they end with headers
                if (Regex.IsMatch(restoredText, @"\*\*\s*[^*]+\s*\*\*\s*$"))
                {
                    restoredText = Regex.Replace(restoredText, @"\*\*\s*[^*]+\s*\*\*\s*$", "").Trim();
                }

                if (!string.IsNullOrEmpty(restoredText))
                {
                    finalizedChunks.Add(restoredText);
                }
            }

            return Task.FromResult(finalizedChunks);
        }

        // Old behavior: token limit only, delegated to Semantic Kernel (unchanged).
        private static List<string> ChunkTokensOnly(string cleanedText, int maxTokens, int overlap)
        {
            var lines = TextChunker.SplitPlainTextLines(cleanedText, maxTokensPerLine: 350);
            return TextChunker.SplitPlainTextParagraphs(lines, maxTokens, overlap);
        }

        // AND semantics: a chunk must satisfy every ENABLED chunk-level limit at once.
        // Stage 1 pre-splits oversized paragraphs (words/paragraph); Stage 2 assembles chunks.
        private List<string> ChunkWithLimits(string cleanedText, ChunkConfig c)
        {
            var paragraphs = SplitParagraphs(cleanedText);

            // Stage 1: paragraph pre-split by words/paragraph.
            if (c.WordsPerParagraphEnabled && c.WordsPerParagraph > 0)
            {
                var split = new List<string>();
                foreach (var p in paragraphs)
                    split.AddRange(SplitByWordCap(p, c.WordsPerParagraph));
                paragraphs = split;
            }

            // Stage 2: accumulate units into chunks; close before any enabled ceiling is breached.
            var chunks = new List<string>();
            var current = new List<string>();

            foreach (var para in paragraphs)
            {
                if (current.Count > 0 && Breaches(current, para, c))
                {
                    chunks.Add(string.Join("\n", current));
                    current = CarryOverlap(current, c.Overlap);
                }
                current.Add(para);
            }
            if (current.Count > 0) chunks.Add(string.Join("\n", current));

            return chunks;
        }

        private static List<string> SplitParagraphs(string text)
            => Regex.Split(text, @"\n+")
                    .Select(p => p.Trim())
                    .Where(p => p.Length > 0)
                    .ToList();

        // Split one paragraph into sub-paragraphs of at most maxWords words each.
        private static IEnumerable<string> SplitByWordCap(string paragraph, int maxWords)
        {
            var words = paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length <= maxWords) return new[] { paragraph };

            var parts = new List<string>();
            for (int i = 0; i < words.Length; i += maxWords)
                parts.Add(string.Join(' ', words.Skip(i).Take(maxWords)));
            return parts;
        }

        // Would appending `next` to `current` breach any enabled chunk-level limit?
        private static bool Breaches(List<string> current, string next, ChunkConfig c)
        {
            if (c.ParagraphsPerChunkEnabled && current.Count + 1 > c.ParagraphsPerChunk)
                return true;

            // Only join when a length limit is on (avoids building the string needlessly).
            if (c.WordsPerChunkEnabled || c.CharsPerChunkEnabled || c.TokensPerChunkEnabled)
            {
                string combined = string.Join("\n", current) + "\n" + next;

                if (c.CharsPerChunkEnabled && combined.Length > c.CharsPerChunk) return true;
                if (c.WordsPerChunkEnabled && CountWords(combined) > c.WordsPerChunk) return true;
                if (c.TokensPerChunkEnabled && EstimateTokens(combined) > c.TokensPerChunk) return true;
            }
            return false;
        }

        // Carry the trailing `overlapWords` words of the closed chunk into the next one.
        // ponytail: overlap approximated in words, carried as one seed unit; good enough for RAG recall.
        private static List<string> CarryOverlap(List<string> closedChunk, int overlapWords)
        {
            if (overlapWords <= 0) return new List<string>();

            var words = string.Join("\n", closedChunk).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0) return new List<string>();

            var tail = words.Skip(Math.Max(0, words.Length - overlapWords));
            return new List<string> { string.Join(' ', tail) };
        }

        private static int CountWords(string text)
            => text.Split(new[] { ' ', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;

        // ponytail: ~4 chars/token heuristic (OpenAI/Gemini rule of thumb); swap for a real tokenizer if cost accounting needs it.
        private static int EstimateTokens(string text) => (text.Length + 3) / 4;
    }
}
