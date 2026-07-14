using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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

        public async Task<IActionResult> OnPostAssignLecturerAsync(int subjectId, int? lecturerId)
        {
            var user = await GetCurrentUser();
            if (user == null) return Unauthorized();

            // Chỉ cho phép thao tác trên môn thuộc khoa của HoD
            var subject = await _subjectService.GetByIdAsync(subjectId);
            if (subject == null || subject.DepartmentId != user.DepartmentId)
                return Forbid();

            // lecturerId phải là GV cùng khoa (hoặc null = gỡ)
            if (lecturerId.HasValue)
            {
                var deptLecturers = await GetDepartmentLecturers(user.DepartmentId);
                if (!deptLecturers.Any(l => l.Id == lecturerId.Value))
                    return Forbid();
            }

            var fromName = subject.LecturerName;
            var ok = await _subjectService.AssignLecturerAsync(subjectId, lecturerId);
            if (ok)
            {
                var toName = "Chưa gán";
                if (lecturerId.HasValue)
                {
                    var updated = await _subjectService.GetByIdAsync(subjectId);
                    toName = updated?.LecturerName ?? "Chưa gán";
                }
                await _auditLogService.LogAsync(
                    user.Id,
                    "AssignLecturer",
                    subjectId.ToString(),
                    $"Môn '{subject.Code} - {subject.Name}': giảng viên '{fromName}' → '{toName}'");

                TempData["Success"] = "Phân công giảng viên thành công.";
            }

            return RedirectToPage();
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
