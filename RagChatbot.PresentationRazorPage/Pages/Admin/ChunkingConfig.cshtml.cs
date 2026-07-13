using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RagChatbot.Business.DTOs;
using RagChatbot.Business.Interfaces;

namespace RagChatbot.PresentationRazorPage.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class ChunkingConfigModel : PageModel
    {
        private readonly ISettingService _settingService;
        private readonly IAuditLogService _auditLogService;

        public ChunkingConfigModel(ISettingService settingService, IAuditLogService auditLogService)
        {
            _settingService = settingService;
            _auditLogService = auditLogService;
        }

        [BindProperty]
        public ChunkConfig Config { get; set; } = new ChunkConfig();

        [TempData]
        public string? SuccessMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            Config = await _settingService.GetChunkConfigAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Mỗi trần chỉ có ý nghĩa khi checkbox tương ứng đang bật; giá trị số của
            // trần đang TẮT sẽ bị bỏ qua hoàn toàn ở TextChunkingService, nên validate
            // ở đây chỉ áp cho các trần đang bật để tránh lưu cấu hình vô nghĩa (0 hoặc âm).
            if (Config.WordsPerChunkEnabled && Config.WordsPerChunk <= 0)
                ModelState.AddModelError(string.Empty, "Số từ / chunk phải lớn hơn 0.");

            if (Config.CharsPerChunkEnabled && Config.CharsPerChunk <= 0)
                ModelState.AddModelError(string.Empty, "Số ký tự / chunk phải lớn hơn 0.");

            if (Config.WordsPerParagraphEnabled && Config.WordsPerParagraph <= 0)
                ModelState.AddModelError(string.Empty, "Số từ / đoạn phải lớn hơn 0.");

            if (Config.ParagraphsPerChunkEnabled && Config.ParagraphsPerChunk <= 0)
                ModelState.AddModelError(string.Empty, "Số đoạn / chunk phải lớn hơn 0.");

            if (Config.TokensPerChunkEnabled && Config.TokensPerChunk <= 0)
                ModelState.AddModelError(string.Empty, "Số token / chunk phải lớn hơn 0.");

            if (Config.Overlap < 0)
                ModelState.AddModelError(string.Empty, "Overlap không được âm.");

            // Phải bật ít nhất 1 trần cấp độ chunk, nếu không TextChunkingService không có
            // điều kiện nào để đóng chunk (WordsPerParagraph chỉ là tiền xử lý, không tính).
            if (!Config.WordsPerChunkEnabled && !Config.CharsPerChunkEnabled &&
                !Config.ParagraphsPerChunkEnabled && !Config.TokensPerChunkEnabled)
            {
                ModelState.AddModelError(string.Empty, "Phải bật ít nhất 1 trần cấp độ chunk (từ / ký tự / đoạn / token).");
            }

            if (!ModelState.IsValid)
            {
                ErrorMessage = "Cấu hình chưa hợp lệ, vui lòng kiểm tra lại các ô đã bật.";
                return Page();
            }

            await _settingService.SaveChunkConfigAsync(Config);

            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var detail = $"Words={Config.WordsPerChunkEnabled}:{Config.WordsPerChunk}, " +
                         $"Chars={Config.CharsPerChunkEnabled}:{Config.CharsPerChunk}, " +
                         $"WordsPerPara={Config.WordsPerParagraphEnabled}:{Config.WordsPerParagraph}, " +
                         $"ParasPerChunk={Config.ParagraphsPerChunkEnabled}:{Config.ParagraphsPerChunk}, " +
                         $"Tokens={Config.TokensPerChunkEnabled}:{Config.TokensPerChunk}, " +
                         $"Overlap={Config.Overlap}";
            await _auditLogService.LogAsync(adminId, "UpdateChunkConfig", "AppSetting", detail);

            SuccessMessage = "Đã lưu cấu hình Chunking thành công. Các thay đổi sẽ áp dụng cho tài liệu upload mới.";
            return RedirectToPage();
        }
    }
}
