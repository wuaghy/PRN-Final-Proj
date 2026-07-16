using RagChatbot.Business.DTOs;

namespace RagChatbot.Business.Interfaces
{
    public interface ISubjectService
    {
        Task<SubjectDto?> GetByIdAsync(int id);
        Task<IEnumerable<SubjectDto>> GetAllByUserIdAsync(int userId);
        Task<IEnumerable<SubjectDto>> GetByDepartmentIdAsync(int departmentId);
        Task<IEnumerable<SubjectDto>> GetAllAsync();
        Task<SubjectDto> AddAsync(CreateSubjectDto dto);
        Task UpdateAsync(SubjectDto subjectDto);
        Task DeleteAsync(int id);
        Task<bool> ExistsByCodeAsync(string code, int? excludeId = null);

        Task ToggleStatusAsync(int id);

        /// <summary>Gán/gỡ giảng viên cho môn học. lecturerId null = gỡ. Trả về false nếu không tìm thấy môn.</summary>
        Task<bool> AssignLecturerAsync(int subjectId, int? lecturerId);
        Task<LecturerAssignmentResultDto> AssignLecturerAsync(int subjectId, int? lecturerId, bool removeCurrentSubjectDocuments);
        Task<LecturerBatchAssignmentResultDto> AssignLecturersAsync(IEnumerable<LecturerAssignmentRequestDto> assignments);

        Task<IEnumerable<SubjectTermDto>> GetSubjectTermHistoryAsync(int subjectId);
    }
}
