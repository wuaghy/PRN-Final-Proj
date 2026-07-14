using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RagChatbot.Business.Interfaces;
using RagChatbot.PresentationRazorPage.Helpers;

namespace RagChatbot.PresentationRazorPage.Pages
{
    [Authorize(Roles = "Student")]
    public class PremiumModel : PageModel
    {
        private readonly IAppUserService _userService;

        public PremiumModel(IAppUserService userService)
        {
            _userService = userService;
        }

        public RagChatbot.Business.DTOs.AppUserDto? CurrentUserDto { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out int userId))
            {
                CurrentUserDto = await _userService.GetByIdAsync(userId);
            }

            if (CurrentUserDto == null)
            {
                return RedirectToPage("/Auth/Login");
            }

            return Page();
        }

        public IActionResult OnPostPayPremium()
        {
            string vnp_Url = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
            string vnp_TmnCode = "QGEPANHQ";
            string vnp_HashSecret = "INOS733PMZQZR8KO4EH5VG8L0TKPRQ66";
            string baseUrl = $"{Request.Scheme}://{Request.Host}";
            string vnp_ReturnUrl = $"{baseUrl}/Wallet?handler=VnpayReturn";

            var vnpay = new VnPayLibrary();

            string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            if (ipAddress == "::1")
            {
                ipAddress = "127.0.0.1";
            }

            vnpay.AddRequestData("vnp_Version", "2.1.0");
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
            vnpay.AddRequestData("vnp_Amount", (50000 * 100).ToString()); // 50,000 VND
            vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_IpAddr", ipAddress);
            vnpay.AddRequestData("vnp_Locale", "vn");
            vnpay.AddRequestData("vnp_OrderInfo", "ThanhToanPremium");
            vnpay.AddRequestData("vnp_OrderType", "other");
            vnpay.AddRequestData("vnp_ReturnUrl", vnp_ReturnUrl);
            vnpay.AddRequestData("vnp_TxnRef", DateTime.Now.Ticks.ToString());

            string paymentUrl = vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);

            return Redirect(paymentUrl);
        }
    }
}
