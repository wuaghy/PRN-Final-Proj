using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RagChatbot.Business.Interfaces;
using RagChatbot.Business.DTOs;
using System.Security.Claims;

namespace RagChatbot.PresentationRazorPage.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class LecturersModel : PageModel
    {
        private readonly IAppUserService _userService;
        private readonly IDepartmentService _departmentService;
        private readonly ISubjectService _subjectService;
        private readonly IAuthService _authService;
        private readonly IAuditLogService _auditLogService;

        public LecturersModel(
            IAppUserService userService,
            IDepartmentService departmentService,
            ISubjectService subjectService,
            IAuthService authService,
            IAuditLogService auditLogService)
        {
            _userService = userService;
            _departmentService = departmentService;
            _subjectService = subjectService;
            _authService = authService;
            _auditLogService = auditLogService;
        }

        public IEnumerable<AppUserDto> Users { get; set; } = default!;
        public IEnumerable<AppUserDto> BannedUsers { get; set; } = default!;
        public IEnumerable<DepartmentDto> Departments { get; set; } = default!;
        public IEnumerable<SubjectDto> AllSubjects { get; set; } = default!;
        public Dictionary<int, List<SubjectDto>> LecturerSubjects { get; set; } = new();

        public int? SelectedDepartmentId { get; set; }
        public int? SelectedSubjectId { get; set; }
        public string SearchEmail { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(int? departmentId, int? subjectId, string searchEmail = "")
        {
            SelectedDepartmentId = departmentId;
            SelectedSubjectId = subjectId;
            SearchEmail = searchEmail ?? string.Empty;

            // Load all lecturers
            var activeLecturers = (await _userService.GetUsersAsync("Lecturer", true, SearchEmail)).ToList();
            var bannedLecturers = (await _userService.GetUsersAsync("Lecturer", false, SearchEmail)).ToList();

            // Load filter lists
            Departments = await _departmentService.GetAllDepartmentsAsync();
            AllSubjects = await _subjectService.GetAllAsync();

            // Group subjects by lecturer
            LecturerSubjects = AllSubjects
                .Where(s => s.LecturerId.HasValue)
                .GroupBy(s => s.LecturerId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Apply department filter
            if (departmentId.HasValue && departmentId > 0)
            {
                activeLecturers = activeLecturers.Where(u => u.DepartmentId == departmentId.Value).ToList();
                bannedLecturers = bannedLecturers.Where(u => u.DepartmentId == departmentId.Value).ToList();
            }

            // Apply subject filter
            if (subjectId.HasValue && subjectId > 0)
            {
                var targetSub = AllSubjects.FirstOrDefault(s => s.Id == subjectId.Value);
                var lecturerId = targetSub?.LecturerId;
                
                activeLecturers = activeLecturers.Where(u => u.Id == lecturerId).ToList();
                bannedLecturers = bannedLecturers.Where(u => u.Id == lecturerId).ToList();
            }

            Users = activeLecturers;
            BannedUsers = bannedLecturers;

            return Page();
        }

        public async Task<IActionResult> OnPostCreateSingleLecturerAsync(string email, string firstName, string lastName, string password, int departmentId)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(password) || departmentId <= 0)
            {
                TempData["Error"] = "Vui lòng nhập đầy đủ thông tin giảng viên và chọn bộ môn.";
                return RedirectToPage(new { departmentId = SelectedDepartmentId, subjectId = SelectedSubjectId, searchEmail = SearchEmail });
            }

            var existingUser = await _userService.GetByEmailAsync(email);
            if (existingUser != null)
            {
                TempData["Error"] = "Email đã tồn tại.";
                return RedirectToPage(new { departmentId = SelectedDepartmentId, subjectId = SelectedSubjectId, searchEmail = SearchEmail });
            }

            var success = await _authService.RegisterAsync(email, password, "Lecturer", firstName, lastName);
            if (!success)
            {
                TempData["Error"] = "Không thể đăng ký tài khoản giảng viên.";
                return RedirectToPage(new { departmentId = SelectedDepartmentId, subjectId = SelectedSubjectId, searchEmail = SearchEmail });
            }

            var createdUser = await _userService.GetByEmailAsync(email);
            if (createdUser != null)
            {
                createdUser.DepartmentId = departmentId;
                await _userService.UpdateUserAsync(createdUser);

                // Admin subject assignment intentionally disabled. HOD owns lecturer assignment.
                // Previous implementation assigned every submitted subjectId here.

                var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                await _auditLogService.LogAsync(adminId, "Create Lecturer", createdUser.Id.ToString(), $"Email: {email}, DeptId: {departmentId}");

                var emailQueue = HttpContext.RequestServices.GetService(typeof(IEmailQueue)) as IEmailQueue;
                if (emailQueue != null)
                {
                    var htmlBody = GetWelcomeEmailHtml(firstName, lastName, email, password);
                    await emailQueue.QueueEmailAsync(new EmailMessage(
                        email,
                        "Thông tin tài khoản Giảng viên RAG Chatbot",
                        htmlBody
                    ));
                }
            }

            TempData["Success"] = "Tạo tài khoản giảng viên thành công.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostCreateLecturersBulkAsync(Microsoft.AspNetCore.Http.IFormFile file, int departmentId)
        {
            if (file == null || file.Length == 0 || departmentId <= 0)
            {
                TempData["Error"] = "Vui lòng chọn file dữ liệu và chọn bộ môn.";
                return RedirectToPage();
            }

            bool skipEmail = file.FileName.Contains("10_hoc_sinh") || file.FileName.Contains("3_giang_vien");

            using var reader = new System.IO.StreamReader(file.OpenReadStream());
            var userData = await reader.ReadToEndAsync();
            var lines = userData.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            int successCount = 0;
            int failCount = 0;
            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var emailQueue = HttpContext.RequestServices.GetService(typeof(IEmailQueue)) as IEmailQueue;

            foreach (var line in lines)
            {
                var parts = line.Split(',', System.StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 3)
                {
                    failCount++;
                    continue;
                }

                var email = parts[0].Trim();
                var lastName = parts[1].Trim();
                var firstName = string.Join(" ", parts.Skip(2)).Trim();
                var password = "123456";

                var success = await _authService.RegisterAsync(email, password, "Lecturer", firstName, lastName);
                if (success)
                {
                    var createdUser = await _userService.GetByEmailAsync(email);
                    if (createdUser != null)
                    {
                        createdUser.DepartmentId = departmentId;
                        await _userService.UpdateUserAsync(createdUser);
                    }

                    if (emailQueue != null && !skipEmail)
                    {
                        var htmlBody = GetWelcomeEmailHtml(firstName, lastName, email, password);
                        await emailQueue.QueueEmailAsync(new EmailMessage(
                            email,
                            "Thông tin tài khoản Giảng viên RAG Chatbot",
                            htmlBody
                        ));
                    }
                    successCount++;
                    await _auditLogService.LogAsync(adminId, "Create Lecturer (Bulk)", "", $"Email: {email}, DeptId: {departmentId}");
                }
                else
                {
                    failCount++;
                }
            }

            if (failCount > 0)
            {
                TempData["Success"] = $"Tạo thành công {successCount} giảng viên. Bỏ qua {failCount} dòng bị trùng email hoặc lỗi.";
            }
            else
            {
                TempData["Success"] = $"Đã tạo thành công toàn bộ {successCount} giảng viên.";
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditLecturerAsync(int id, string firstName, string lastName, int departmentId)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null || user.Role != "Lecturer")
            {
                TempData["Error"] = "Không tìm thấy giảng viên.";
                return RedirectToPage();
            }

            if (user.DepartmentId.HasValue && user.DepartmentId.Value != departmentId)
            {
                var allSubjects = await _subjectService.GetAllAsync();
                var managedSubjects = allSubjects.Where(s => s.LecturerId == id).ToList();
                if (managedSubjects.Any())
                {
                    var subjectNames = string.Join(", ", managedSubjects.Select(s => s.Code));
                    TempData["Error"] = $"Không thể chuyển bộ môn. Giảng viên đang quản lý các môn học thuộc bộ môn cũ: {subjectNames}. Vui lòng gỡ phân công các môn học này trước khi chuyển bộ môn.";
                    return RedirectToPage();
                }
            }

            user.FirstName = firstName;
            user.LastName = lastName;
            user.DepartmentId = departmentId;

            await _userService.UpdateUserAsync(user);

            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            await _auditLogService.LogAsync(adminId, "Update Lecturer Details", id.ToString(), $"Email: {user.Email}, DeptId: {departmentId}");

            TempData["Success"] = "Cập nhật thông tin giảng viên thành công.";
            return RedirectToPage();
        }

        // Admin subject assignment intentionally disabled. Keep this implementation for reuse.
#if false
        public async Task<IActionResult> OnPostChangeSubjectsAsync(int id, List<int> subjectIds)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null || user.Role != "Lecturer")
            {
                TempData["Error"] = "Không tìm thấy giảng viên.";
                return RedirectToPage();
            }

            if (subjectIds != null && subjectIds.Any())
            {
                if (!user.DepartmentId.HasValue)
                {
                    TempData["Error"] = "Giảng viên chưa được phân vào bộ môn nào, không thể phân công môn giảng dạy.";
                    return RedirectToPage();
                }

                var allSubjects = await _subjectService.GetAllAsync();
                foreach (var subId in subjectIds)
                {
                    var sub = allSubjects.FirstOrDefault(s => s.Id == subId);
                    if (sub == null)
                    {
                        TempData["Error"] = "Môn học không tồn tại.";
                        return RedirectToPage();
                    }
                    if (sub.DepartmentId != user.DepartmentId)
                    {
                        TempData["Error"] = $"Không thể phân công môn học '{sub.Code}' cho giảng viên này vì môn học thuộc bộ môn khác.";
                        return RedirectToPage();
                    }
                }
            }

            var allSubjectsForRemoval = await _subjectService.GetAllAsync();
            var currentlyAssigned = allSubjectsForRemoval.Where(s => s.LecturerId == id).ToList();

            foreach (var sub in currentlyAssigned)
            {
                if (subjectIds == null || !subjectIds.Contains(sub.Id))
                {
                    await _subjectService.AssignLecturerAsync(sub.Id, null);
                }
            }

            if (subjectIds != null)
            {
                foreach (var subId in subjectIds)
                {
                    await _subjectService.AssignLecturerAsync(subId, id);
                }
            }

            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            await _auditLogService.LogAsync(adminId, "Change Lecturer Subjects", id.ToString(), $"Email: {user.Email}, AssignedCount: {(subjectIds?.Count ?? 0)}");

            TempData["Success"] = "Cập nhật danh sách môn học giảng dạy thành công.";
            return RedirectToPage();
        }
#endif

        public async Task<IActionResult> OnPostDeleteLecturerAsync(int id)
        {
            var userDto = await _userService.GetByIdAsync(id);
            if (userDto != null && userDto.Role == "Lecturer")
            {
                await _userService.SoftDeleteUserAsync(id);

                var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                await _auditLogService.LogAsync(adminId, "Soft Delete Lecturer", id.ToString(), $"Email: {userDto.Email}");

                var emailQueue = HttpContext.RequestServices.GetService(typeof(IEmailQueue)) as IEmailQueue;
                if (emailQueue != null)
                {
                    var htmlBody = GetAccountLockedEmailHtml(userDto.FirstName, userDto.LastName, userDto.Email);
                    await emailQueue.QueueEmailAsync(new EmailMessage(
                        userDto.Email,
                        "Tài khoản của bạn đã bị vô hiệu hóa",
                        htmlBody
                    ));
                }

                TempData["Success"] = $"Đã khóa tài khoản giảng viên {userDto.Email}.";
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRestoreLecturerAsync(int id)
        {
            var userDto = await _userService.GetByIdAsync(id);
            if (userDto != null && userDto.Role == "Lecturer")
            {
                await _userService.RestoreUserAsync(id);

                var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                await _auditLogService.LogAsync(adminId, "Restore Lecturer", id.ToString(), $"Email: {userDto.Email}");

                var emailQueue = HttpContext.RequestServices.GetService(typeof(IEmailQueue)) as IEmailQueue;
                if (emailQueue != null)
                {
                    var htmlBody = GetAccountRestoredEmailHtml(userDto.FirstName, userDto.LastName, userDto.Email);
                    await emailQueue.QueueEmailAsync(new EmailMessage(
                        userDto.Email,
                        "Tài khoản của bạn đã được khôi phục",
                        htmlBody
                    ));
                }

                TempData["Success"] = $"Đã khôi phục tài khoản giảng viên {userDto.Email}.";
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostResetPasswordAsync(int id)
        {
            var userDto = await _userService.GetByIdAsync(id);
            if (userDto != null && userDto.Role == "Lecturer")
            {
                await _userService.ResetPasswordAsync(id, "123456");

                var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                await _auditLogService.LogAsync(adminId, "Reset Password Lecturer", id.ToString(), $"Email: {userDto.Email}");

                TempData["Success"] = $"Đặt lại mật khẩu thành công cho giảng viên {userDto.Email} (Mật khẩu mới: 123456).";
            }
            else
            {
                TempData["Error"] = "Không tìm thấy tài khoản hoặc không được phép đổi mật khẩu.";
            }
            return RedirectToPage();
        }

        private string GetWelcomeEmailHtml(string firstName, string lastName, string email, string password)
        {
            return $@"
                <div style=""font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; max-width: 600px; margin: 0 auto; padding: 30px; border: 1px solid #e2e8f0; border-radius: 12px; background-color: #ffffff; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);"">
                    <div style=""text-align: center; padding-bottom: 25px; border-bottom: 2px solid #10b981;"">
                        <h2 style=""color: #0f172a; margin: 0; font-size: 24px;"">RAG Chatbot System</h2>
                        <p style=""color: #64748b; margin-top: 8px; font-size: 14px;"">Hệ thống Trợ lý ảo & Quản lý Tài liệu</p>
                    </div>
                    <div style=""padding: 30px 0; color: #334155; line-height: 1.6;"">
                        <p style=""font-size: 16px; margin-bottom: 20px;"">Xin chào Thầy/Cô <strong>{lastName} {firstName}</strong>,</p>
                        <p style=""font-size: 15px;"">Tài khoản giảng viên của bạn đã được quản trị viên khởi tạo thành công. Dưới đây là thông tin đăng nhập dành cho bạn:</p>
                        <div style=""background-color: #f8fafc; border: 1px solid #e2e8f0; padding: 20px; border-radius: 8px; margin: 25px 0;"">
                            <p style=""margin: 0 0 10px 0; font-size: 15px;""><strong>Email:</strong> <span style=""color: #0f172a;"">{email}</span></p>
                            <p style=""margin: 0; font-size: 15px;""><strong>Mật khẩu mặc định:</strong> <span style=""color: #0f172a; font-family: monospace; font-size: 16px; background-color: #e2e8f0; padding: 2px 6px; border-radius: 4px;"">{password}</span></p>
                        </div>
                        <p style=""color: #dc2626; font-size: 14px; background-color: #fef2f2; padding: 12px; border-radius: 8px; border-left: 4px solid #dc2626;""><strong>Lưu ý quan trọng:</strong> Đây là mật khẩu mặc định. Để bảo đảm an toàn, vui lòng đăng nhập và tiến hành đổi mật khẩu ngay lập tức tại phần <strong>Hồ sơ cá nhân</strong>.</p>
                    </div>
                    <div style=""text-align: center; padding-top: 25px; border-top: 1px solid #e2e8f0;"">
                        <a href=""https://localhost:5186/Auth/Login"" style=""display: inline-block; background-color: #10b981; color: #ffffff; text-decoration: none; padding: 12px 28px; border-radius: 8px; font-weight: 600; font-size: 15px; box-shadow: 0 2px 4px rgba(16, 185, 129, 0.3);"">Đăng nhập vào Hệ thống</a>
                    </div>
                </div>";
        }

        private string GetAccountLockedEmailHtml(string firstName, string lastName, string email)
        {
            return $@"
                <div style=""font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; max-width: 600px; margin: 0 auto; padding: 30px; border: 1px solid #fecdd3; border-radius: 12px; background-color: #ffffff; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);"">
                    <div style=""text-align: center; padding-bottom: 25px; border-bottom: 2px solid #e11d48;"">
                        <h2 style=""color: #0f172a; margin: 0; font-size: 24px;"">Thông báo Khóa tài khoản</h2>
                    </div>
                    <div style=""padding: 30px 0; color: #334155; line-height: 1.6;"">
                        <p style=""font-size: 16px; margin-bottom: 20px;"">Xin chào Thầy/Cô <strong>{lastName} {firstName}</strong>,</p>
                        <p style=""font-size: 15px;"">Chúng tôi xin thông báo rằng tài khoản giảng viên của bạn (<strong>{email}</strong>) hiện đã bị <strong>Tạm khóa / Vô hiệu hóa</strong> bởi Quản trị viên.</p>
                        <div style=""background-color: #fff1f2; border: 1px solid #ffe4e6; padding: 15px; border-radius: 8px; margin: 25px 0; color: #be123c;"">
                            <p style=""margin: 0; font-size: 15px;"">Bạn sẽ không thể đăng nhập hoặc tiếp tục sử dụng các dịch vụ của hệ thống RAG Chatbot cho đến khi tài khoản được mở khóa.</p>
                        </div>
                    </div>
                    <div style=""text-align: center; padding-top: 25px; border-top: 1px solid #e2e8f0; font-size: 13px; color: #64748b;"">
                        Đây là email tự động, vui lòng không trả lời.
                    </div>
                </div>";
        }

        private string GetAccountRestoredEmailHtml(string firstName, string lastName, string email)
        {
            return $@"
                <div style=""font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; max-width: 600px; margin: 0 auto; padding: 30px; border: 1px solid #e2e8f0; border-radius: 12px; background-color: #ffffff; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);"">
                    <div style=""text-align: center; padding-bottom: 25px; border-bottom: 2px solid #10b981;"">
                        <h2 style=""color: #0f172a; margin: 0; font-size: 24px;"">Tài khoản đã được Khôi phục</h2>
                        <p style=""color: #64748b; margin-top: 8px; font-size: 14px;"">RAG Chatbot System</p>
                    </div>
                    <div style=""padding: 25px 0; color: #334155; line-height: 1.6;"">
                        <p style=""font-size: 16px; margin-bottom: 20px;"">Xin chào Thầy/Cô <strong>{lastName} {firstName}</strong>,</p>
                        <p style=""font-size: 15px;"">Tài khoản giảng viên của bạn (<strong>{email}</strong>) đã được <strong>Khôi phục hoạt động</strong> bởi Quản trị viên.</p>
                    </div>
                    <div style=""text-align: center; padding-top: 30px; border-top: 1px solid #e2e8f0;"">
                        <a href=""https://localhost:5186/Auth/Login"" style=""display: inline-block; background-color: #10b981; color: #ffffff; text-decoration: none; padding: 12px 28px; border-radius: 8px; font-weight: 600; font-size: 15px; box-shadow: 0 2px 4px rgba(16, 185, 129, 0.3);"">Đăng nhập vào Hệ thống</a>
                    </div>
                </div>";
        }
    }
}
