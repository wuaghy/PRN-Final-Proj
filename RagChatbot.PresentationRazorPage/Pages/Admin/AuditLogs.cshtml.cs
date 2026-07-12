using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RagChatbot.Business.Interfaces;
using RagChatbot.DataAccess.EntityModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RagChatbot.PresentationRazorPage.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class AuditLogsModel : PageModel
    {
        private readonly IAuditLogService _auditLogService;

        public AuditLogsModel(IAuditLogService auditLogService)
        {
            _auditLogService = auditLogService;
        }

        public IEnumerable<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

        public async Task OnGetAsync()
        {
            AuditLogs = await _auditLogService.GetAllLogsAsync();
        }
    }
}
