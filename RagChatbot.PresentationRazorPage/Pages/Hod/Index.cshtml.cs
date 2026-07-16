using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RagChatbot.Business.DTOs;
using RagChatbot.Business.Interfaces;

namespace RagChatbot.PresentationRazorPage.Pages.Hod
{
    [Authorize(Roles = "HeadOfDepartment")]
    public class IndexModel : PageModel
    {
        private readonly IAppUserService _userService;
        private readonly ISubjectService _subjectService;
        private readonly IAuditLogService _auditLogService;

        public IndexModel(IAppUserService userService, ISubjectService subjectService, IAuditLogService auditLogService)
        {
            _userService = userService;
            _subjectService = subjectService;
            _auditLogService = auditLogService;
        }

        public System.Collections.Generic.IEnumerable<RagChatbot.Business.DTOs.SubjectDto> Subjects { get; set; } = default!;
        public System.Collections.Generic.IEnumerable<RagChatbot.Business.DTOs.AppUserDto> Lecturers { get; set; } = new List<RagChatbot.Business.DTOs.AppUserDto>();

        private async Task<RagChatbot.Business.DTOs.AppUserDto> GetCurrentUser()
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out int userId))
            {
                return await _userService.GetByIdAsync(userId);
            }
            return null!;
        }

        // Giảng viên cùng khoa với HoD (Role=Lecturer, DepartmentId khớp)
        private async Task<IEnumerable<RagChatbot.Business.DTOs.AppUserDto>> GetDepartmentLecturers(int? departmentId)
        {
            var lecturers = await _userService.GetUsersByRoleAsync("Lecturer");
            return lecturers.Where(u => u.DepartmentId == departmentId).ToList();
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await GetCurrentUser();
            if (user == null) return Unauthorized();

            Subjects = await _subjectService.GetByDepartmentIdAsync(user.DepartmentId ?? 0);
            Lecturers = await GetDepartmentLecturers(user.DepartmentId);

            return Page();
        }

        public async Task<IActionResult> OnPostAssignLecturersAsync(List<LecturerAssignmentRequestDto> assignments)
        {
            var user = await GetCurrentUser();
            if (user == null) return Unauthorized();
            if (!user.DepartmentId.HasValue) return Forbid();

            assignments ??= new List<LecturerAssignmentRequestDto>();
            if (assignments.Count == 0)
            {
                TempData["Info"] = "Không có thay đổi phân công nào để lưu.";
                return RedirectToPage();
            }

            if (assignments.GroupBy(assignment => assignment.SubjectId).Any(group => group.Count() > 1))
            {
                TempData["Error"] = "Danh sách phân công chứa môn học bị trùng.";
                return RedirectToPage();
            }

            var departmentSubjects = (await _subjectService.GetByDepartmentIdAsync(user.DepartmentId.Value)).ToList();
            var subjectsById = departmentSubjects.ToDictionary(subject => subject.Id);
            if (assignments.Any(assignment => !subjectsById.ContainsKey(assignment.SubjectId)))
                return Forbid();

            var departmentLecturers = (await GetDepartmentLecturers(user.DepartmentId)).ToList();
            var lecturersById = departmentLecturers.ToDictionary(lecturer => lecturer.Id);
            if (assignments.Any(assignment => assignment.LecturerId.HasValue
                && !lecturersById.ContainsKey(assignment.LecturerId.Value)))
                return Forbid();

            var result = await _subjectService.AssignLecturersAsync(assignments);
            if (!result.Success)
            {
                TempData["Error"] = result.ErrorMessage;
                return RedirectToPage();
            }

            foreach (var assignmentResult in result.Assignments.Where(assignment => assignment.Changed))
            {
                var previousSubject = subjectsById[assignmentResult.SubjectId];
                var previousLecturerName = previousSubject.LecturerName;
                var newLecturerName = assignmentResult.NewLecturerId.HasValue
                    ? FormatLecturerName(lecturersById[assignmentResult.NewLecturerId.Value])
                    : "Chưa gán";

                await _auditLogService.LogAsync(
                    user.Id,
                    "ReassignLecturer",
                    assignmentResult.SubjectId.ToString(),
                    $"Subject={assignmentResult.SubjectCode} - {assignmentResult.SubjectName}; " +
                    $"Lecturer={previousLecturerName} -> {newLecturerName}; " +
                    $"RemoveCurrentSubjectDocuments={assignmentResult.RemoveCurrentSubjectDocumentsRequested}; " +
                    $"DeletedCount={assignmentResult.DeletedDocumentCount}; " +
                    $"DeletedDocumentIds=[{string.Join(',', assignmentResult.DeletedDocumentIds)}]");
            }

            TempData["Success"] = result.ChangedCount == 0
                ? "Không có thay đổi phân công nào để lưu."
                : $"Đã cập nhật {result.ChangedCount} môn học, bỏ qua {result.UnchangedCount} môn không thay đổi" +
                  (result.DeletedDocumentCount > 0
                      ? $" và xóa {result.DeletedDocumentCount} tài liệu hiện tại của môn."
                      : ".");

            return RedirectToPage();
        }

        private static string FormatLecturerName(AppUserDto lecturer)
        {
            return $"{lecturer.LastName} {lecturer.FirstName}".Trim();
        }

        public async Task<IActionResult> OnGetSubjectsDataAsync()
        {
            var user = await GetCurrentUser();
            if (user == null) return Unauthorized();

            var subjects = await _subjectService.GetByDepartmentIdAsync(user.DepartmentId ?? 0);
            var subjectData = subjects.Select(s => new
            {
                s.Code,
                s.Name,
                s.IsActive
            });

            return new JsonResult(subjectData);
        }

        public async Task<IActionResult> OnGetSubjectHistoryAsync(int subjectId)
        {
            var user = await GetCurrentUser();
            if (user == null) return Unauthorized();

            var subject = await _subjectService.GetByIdAsync(subjectId);
            if (subject == null || subject.DepartmentId != user.DepartmentId)
                return Forbid();

            var history = await _subjectService.GetSubjectTermHistoryAsync(subjectId);
            return new JsonResult(history);
        }
    }
}
