using System.Text.RegularExpressions;
using RagChatbot.Business.DTOs;
using RagChatbot.Business.Interfaces;

namespace RagChatbot.Business.Services
{
    public class TextChunkingService : ITextChunkingService
    {
        public Task<List<string>> ChunkTextAsync(string rawText, ChunkConfig config)
        {
            if (string.IsNullOrWhiteSpace(rawText)) return Task.FromResult(new List<string>());

            // Clean repeating layout noise and remove global structural artifacts
            string cleanedText = Regex.Replace(rawText, @"NGUYÊN LÝ KẾ TOÁN\s*\.\s*", " ");

            // Capture and collapse all formatting spaces or layout newlines around numeric periods
            int safety = 0;
            while (safety++ < 100 && Regex.IsMatch(cleanedText, @"(\d+)\s*\.\s*(\d+)"))
            {
                cleanedText = Regex.Replace(cleanedText, @"(\d+)\s*\.\s*(\d+)", "$1ALPHANUMERICDOTMASK$2");
            }

            // Isolate markdown bold tag boundaries from word tokens
            cleanedText = cleanedText.Replace("**", " ** ");

            var chunks = ChunkWithLimits(cleanedText, config);

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

        // AND semantics: a chunk must satisfy every ENABLED chunk-level limit at once.
        // Stage 1 pre-splits oversized paragraphs (words/paragraph); Stage 1.5 guarantees no
        // single unit alone already breaches an enabled chunk-level cap (handles text with no
        // '\n' at all — e.g. a whole extracted page as one block); Stage 2 assembles chunks.
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

            // Stage 1.5: SplitParagraphs only breaks on '\n'. Extracted text (e.g. a whole
            // .docx page) is often a single continuous block with no '\n' at all, so without
            // this step a single oversized "paragraph" would sail through Stage 2 untouched —
            // Breaches() only fires when adding a NEXT unit, never for the very first one.
            // Greedily re-split any unit that alone already exceeds an enabled Words/Chars/
            // Tokens-per-chunk cap, so those caps have effect even with no paragraph breaks.
            if (c.WordsPerChunkEnabled || c.CharsPerChunkEnabled || c.TokensPerChunkEnabled)
            {
                var capped = new List<string>();
                foreach (var p in paragraphs)
                    capped.AddRange(SplitOversizedUnit(p, c));
                paragraphs = capped;
            }

            // Stage 2: accumulate original paragraphs separately from the overlap prefix.
            // Overlap is measured in words, is not counted as a paragraph, and is reduced
            // when necessary so the next chunk still satisfies every enabled size cap.
            var chunks = new List<string>();
            var current = new List<string>();
            string overlapPrefix = string.Empty;

            foreach (var para in paragraphs)
            {
                if (current.Count > 0 && Breaches(overlapPrefix, current, para, c))
                {
                    string closedChunk = JoinChunk(overlapPrefix, current);
                    chunks.Add(closedChunk);
                    overlapPrefix = FitOverlap(closedChunk, para, c);
                    current = new List<string>();
                }
                current.Add(para);
            }
            if (current.Count > 0) chunks.Add(JoinChunk(overlapPrefix, current));

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

        // Greedily walk word-by-word, cutting a new unit whenever adding the next word would
        // breach an enabled Words/Chars/Tokens-per-chunk cap. Mirrors Breaches()'s own checks
        // so a unit produced here is guaranteed to pass Breaches() as the first item of a chunk.
        private static IEnumerable<string> SplitOversizedUnit(string text, ChunkConfig c)
        {
            if (!ExceedsAlone(text, c))
            {
                yield return text;
                yield break;
            }

            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length <= 1)
            {
                yield return text; // single unsplittable token (e.g. one giant word/URL) — best effort
                yield break;
            }

            var current = new List<string>();
            foreach (var w in words)
            {
                if (current.Count > 0)
                {
                    var candidate = string.Join(' ', current) + " " + w;
                    if (ExceedsAlone(candidate, c))
                    {
                        yield return string.Join(' ', current);
                        current = new List<string> { w };
                        continue;
                    }
                }
                current.Add(w);
            }
            if (current.Count > 0) yield return string.Join(' ', current);
        }

        // Would this text alone (as the first/only item of a new chunk) already breach an
        // enabled Words/Chars/Tokens-per-chunk cap? (ParagraphsPerChunk is not a size cap.)
        private static bool ExceedsAlone(string text, ChunkConfig c)
        {
            if (c.CharsPerChunkEnabled && text.Length > c.CharsPerChunk) return true;
            if (c.WordsPerChunkEnabled && CountWords(text) > c.WordsPerChunk) return true;
            if (c.TokensPerChunkEnabled && EstimateTokens(text) > c.TokensPerChunk) return true;
            return false;
        }

        // Would appending `next` breach a paragraph count or any enabled size limit?
        // The overlap prefix is content, but it is not an original paragraph.
        private static bool Breaches(string overlapPrefix, List<string> current, string next, ChunkConfig c)
        {
            if (c.ParagraphsPerChunkEnabled && current.Count + 1 > c.ParagraphsPerChunk)
                return true;

            if (c.WordsPerChunkEnabled || c.CharsPerChunkEnabled || c.TokensPerChunkEnabled)
            {
                string combined = JoinChunk(overlapPrefix, current.Append(next));
                return ExceedsChunkLevel(combined, c);
            }
            return false;
        }

        private static string FitOverlap(string closedChunk, string nextParagraph, ChunkConfig c)
        {
            if (c.Overlap <= 0) return string.Empty;

            var words = closedChunk.Split(
                new[] { ' ', '\n', '\t' },
                StringSplitOptions.RemoveEmptyEntries);
            int maximum = Math.Min(c.Overlap, words.Length);

            int low = 1;
            int high = maximum;
            string best = string.Empty;
            while (low <= high)
            {
                int wordCount = low + (high - low) / 2;
                string candidate = string.Join(' ', words.Skip(words.Length - wordCount));
                string combined = JoinChunk(candidate, new[] { nextParagraph });
                if (!ExceedsChunkLevel(combined, c))
                {
                    best = candidate;
                    low = wordCount + 1;
                }
                else
                {
                    high = wordCount - 1;
                }
            }

            return best;
        }

        private static bool ExceedsChunkLevel(string text, ChunkConfig c)
        {
            if (c.CharsPerChunkEnabled && text.Length > c.CharsPerChunk) return true;
            if (c.WordsPerChunkEnabled && CountWords(text) > c.WordsPerChunk) return true;
            if (c.TokensPerChunkEnabled && EstimateTokens(text) > c.TokensPerChunk) return true;
            return false;
        }

        private static string JoinChunk(string overlapPrefix, IEnumerable<string> paragraphs)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(overlapPrefix)) parts.Add(overlapPrefix.Trim());
            parts.AddRange(paragraphs.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p.Trim()));
            return string.Join("\n", parts);
        }

        private static int CountWords(string text)
            => text.Split(new[] { ' ', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;

        // ponytail: ~4 chars/token heuristic (OpenAI/Gemini rule of thumb); swap for a real tokenizer if cost accounting needs it.
        private static int EstimateTokens(string text) => (text.Length + 3) / 4;
    }
}
