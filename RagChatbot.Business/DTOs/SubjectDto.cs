namespace RagChatbot.Business.DTOs
{
    public class SubjectDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string ManagerName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int? DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public int? LecturerId { get; set; }
        public string LecturerName { get; set; } = "Chưa gán";
        public ICollection<DocumentDto> Documents { get; set; } = new List<DocumentDto>();
    }

    public class CreateSubjectDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int? DepartmentId { get; set; }
    }

    public class LecturerAssignmentResultDto
    {
        public int SubjectId { get; set; }
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public bool Success { get; set; }
        public bool Changed { get; set; }
        public int? PreviousLecturerId { get; set; }
        public int? NewLecturerId { get; set; }
        public bool RemoveCurrentSubjectDocumentsRequested { get; set; }
        public List<int> DeletedDocumentIds { get; set; } = new();
        public int DeletedDocumentCount => DeletedDocumentIds.Count;
    }

    public class LecturerAssignmentRequestDto
    {
        public int SubjectId { get; set; }
        public int? OriginalLecturerId { get; set; }
        public int? LecturerId { get; set; }
        public bool RemoveCurrentSubjectDocuments { get; set; }
    }

    public class LecturerBatchAssignmentResultDto
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public int UnchangedCount { get; set; }
        public List<LecturerAssignmentResultDto> Assignments { get; set; } = new();
        public int ChangedCount => Assignments.Count(assignment => assignment.Changed);
        public int DeletedDocumentCount => Assignments.Sum(assignment => assignment.DeletedDocumentCount);
    }
}
