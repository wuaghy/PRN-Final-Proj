using System;

namespace RagChatbot.Business.DTOs
{
    public class SubjectTermDto
    {
        public string LecturerName { get; set; } = string.Empty;
        public string StartAt { get; set; } = string.Empty;
        public string? EndAt { get; set; }
    }
}
