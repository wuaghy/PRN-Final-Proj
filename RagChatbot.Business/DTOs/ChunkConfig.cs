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

        // Tokens/chunk is estimated consistently by the custom chunker.
        public bool TokensPerChunkEnabled { get; set; } = true;
        public int TokensPerChunk { get; set; } = 400;

        // Number of trailing words carried into the next chunk.
        public int Overlap { get; set; } = 50;
    }
}
