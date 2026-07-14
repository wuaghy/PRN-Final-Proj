using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RagChatbot.Business.Interfaces;
using RagChatbot.PresentationRazorPage.Helpers;

namespace RagChatbot.PresentationRazorPage.Pages.Wallet
{
    public class IndexModel : PageModel
    {
        private readonly ITransactionService _transactionService;
        private readonly IAppUserService _appUserService;

        public IndexModel(ITransactionService transactionService, IAppUserService appUserService)
        {
            _transactionService = transactionService;
            _appUserService = appUserService;
        }

        public RagChatbot.Business.DTOs.AppUserDto? UserStats { get; set; }

        public async Task OnGetAsync()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out int uId))
            {
                UserStats = await _appUserService.GetUserTokenStatsAsync(uId);
            }
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

        public async Task<IActionResult> OnGetVnpayReturnAsync()
        {
            string vnp_HashSecret = "INOS733PMZQZR8KO4EH5VG8L0TKPRQ66";
            var vnpay = new VnPayLibrary();

            foreach (var key in Request.Query.Keys)
            {
                if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_") && key != "vnp_SecureHash")
                {
                    vnpay.AddResponseData(key, Request.Query[key].ToString());
                }
            }

            string vnp_SecureHash = Request.Query["vnp_SecureHash"];
            bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, vnp_HashSecret);

            if (checkSignature)
            {
                string responseCode = vnpay.GetResponseData("vnp_ResponseCode");
                if (responseCode == "00")
                {
                    var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (int.TryParse(userIdStr, out int uId))
                    {
                        // LẤY SỐ TIỀN THỰC TẾ TỪ VNPAY (VNPAY nhân 100 nên phải chia lại 100)
                        string rawAmount = vnpay.GetResponseData("vnp_Amount");
                        decimal amountVnd = decimal.TryParse(rawAmount, out var parsedAmt) ? parsedAmt / 100m : 50000m;

                        // Gọi service xử lý cập nhật trạng thái Premium và ghi nhận giao dịch
                        await _transactionService.ProcessPremiumUpgradeAsync(uId, amountVnd);
                    }

                    ViewData["Message"] = "🎉 Thanh toán thành công! Tài khoản của bạn đã được nâng cấp lên Premium. Bạn đã có quyền đọc toàn bộ tài liệu.";
                }
                else
                {
                    ViewData["Message"] = $"❌ Giao dịch thất bại hoặc đã bị hủy bởi người dùng. (Mã lỗi: {responseCode})";
                }
            }
            else
            {
                ViewData["Message"] = "⚠️ Cảnh báo: Chữ ký phản hồi không hợp lệ! Dữ liệu có thể đã bị can thiệp.";
            }

            return Page();
        }
    }
}