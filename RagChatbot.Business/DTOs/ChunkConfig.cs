namespace RagChatbot.Business.DTOs
{
    // Runtime chunking config. Each limit has an on/off flag + a value.
    // A chunk must satisfy ALL enabled chunk-level limits at once (AND).
    // WordsPerParagraph is a pre-split pass on paragraphs, not a chunk limit.
    public class ChunkConfig
    {
        public bool WordsPerChunkEnabled { get; set; }
        public int WordsPerChunk { get; set; } = 300;

        public bool CharsPerChunkEnabled { get; set; }
        public int CharsPerChunk { get; set; } = 2000;

        public bool WordsPerParagraphEnabled { get; set; }
        public int WordsPerParagraph { get; set; } = 150;

        public bool ParagraphsPerChunkEnabled { get; set; }
        public int ParagraphsPerChunk { get; set; } = 5;

        // Tokens/chunk: the original hardcoded maxChunkSize. Default ON so
        // out-of-the-box behavior matches the old code exactly.
        public bool TokensPerChunkEnabled { get; set; } = true;
        public int TokensPerChunk { get; set; } = 400;

        public int Overlap { get; set; } = 50;

        // True when only the token limit is active => old SK-based path, no behavior change.
        public bool IsTokenOnly =>
            TokensPerChunkEnabled
            && !WordsPerChunkEnabled
            && !CharsPerChunkEnabled
            && !WordsPerParagraphEnabled
            && !ParagraphsPerChunkEnabled;
    }
}
