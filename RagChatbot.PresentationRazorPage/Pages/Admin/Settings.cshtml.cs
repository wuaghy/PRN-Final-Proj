using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RagChatbot.Business.DTOs;
using RagChatbot.Business.Interfaces;
using System.Security.Claims;

namespace RagChatbot.PresentationRazorPage.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class SettingsModel : PageModel
    {
        private readonly ISettingService _settingService;
        private readonly IAuditLogService _auditLogService;

        public SettingsModel(ISettingService settingService, IAuditLogService auditLogService)
        {
            _settingService = settingService;
            _auditLogService = auditLogService;
        }

        [BindProperty]
        public PricingConfig Input { get; set; } = new PricingConfig();

        public async Task<IActionResult> OnGetAsync()
        {
            Input = await _settingService.GetPricingConfigAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var actorIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(actorIdValue, out var actorId)) return Unauthorized();

            var previous = await _settingService.GetPricingConfigAsync();
            await _settingService.SavePricingConfigAsync(Input);
            await _auditLogService.LogAsync(
                actorId,
                "UpdatePricingConfig",
                "AppSetting",
                $"UsdVndRate={previous.UsdVndRate}->{Input.UsdVndRate}; " +
                $"TokenInCostPerMillion={previous.TokenInCostPerMillion}->{Input.TokenInCostPerMillion}; " +
                $"TokenOutCostPerMillion={previous.TokenOutCostPerMillion}->{Input.TokenOutCostPerMillion}");
            TempData["Success"] = "Cập nhật cấu hình thành công.";
            return RedirectToPage();
        }
    }
}
