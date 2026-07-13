using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RagChatbot.Business.DTOs;
using RagChatbot.Business.Interfaces;

namespace RagChatbot.PresentationRazorPage.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class SettingsModel : PageModel
    {
        private readonly ISettingService _settingService;

        public SettingsModel(ISettingService settingService)
        {
            _settingService = settingService;
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

            await _settingService.SavePricingConfigAsync(Input);
            TempData["Success"] = "Cập nhật cấu hình thành công.";
            return RedirectToPage();
        }
    }
}
