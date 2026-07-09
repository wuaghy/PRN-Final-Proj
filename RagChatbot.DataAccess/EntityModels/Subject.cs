namespace RagChatbot.DataAccess.EntityModels
{
    public class Subject
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = false;

        public int? DepartmentId { get; set; }

        // 1 subject -> 1 lecturer (nullable); 1 lecturer -> many subjects
        public int? LecturerId { get; set; }

        // Navigation Properties
        public Department? Department { get; set; }
        public AppUser? Lecturer { get; set; }
        public ICollection<Document> Documents { get; set; } = new List<Document>();
        public ICollection<ChatSession> ChatSessions { get; set; } = new List<ChatSession>();
    }
}
