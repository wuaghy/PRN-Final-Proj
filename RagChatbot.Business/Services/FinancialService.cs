using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RagChatbot.Business.DTOs;
using RagChatbot.Business.Interfaces;
using RagChatbot.DataAccess.Data;
using RagChatbot.DataAccess.EntityModels;

namespace RagChatbot.Business.Services
{
    public class FinancialService : IFinancialService
    {
        private readonly ApplicationDbContext _context;
        private readonly ISettingService _settingService;

        public FinancialService(ApplicationDbContext context, ISettingService settingService)
        {
            _context = context;
            _settingService = settingService;
        }

        public async Task<FinancialDashboardDto> GetDashboardDataAsync()
        {
            var dto = new FinancialDashboardDto();

            // 1. Tính tổng doanh thu từ VNPAY
            dto.TotalRevenueVnd = await _context.Transactions.SumAsync(t => t.Amount);

            // 2. Thống kê lượng Token tiêu thụ từ ChatMessage
            var chatMetrics = await _context.ChatMessages
                .Where(m => m.Role == "assistant" && (m.TokenIn != null || m.TokenOut != null))
                .Select(m => new { m.TokenIn, m.TokenOut, m.UsdRate })
                .ToListAsync();

            dto.TotalInputTokens = chatMetrics.Sum(m => (long)(m.TokenIn ?? 0));
            dto.TotalOutputTokens = chatMetrics.Sum(m => (long)(m.TokenOut ?? 0));

            var priceConfig = await _settingService.GetPricingConfigAsync();
            decimal inputCostRate = priceConfig.TokenInCostPerMillion / 1000000m;
            decimal outputCostRate = priceConfig.TokenOutCostPerMillion / 1000000m;

            dto.TotalCostUsd = (dto.TotalInputTokens * inputCostRate) + (dto.TotalOutputTokens * outputCostRate);

            // Tính toán chi phí quy đổi VND theo tỷ giá snapshot tại từng tin nhắn
            dto.TotalCostVnd = chatMetrics.Sum(m =>
            {
                decimal msgCostUsd = ((m.TokenIn ?? 0) * inputCostRate) + ((m.TokenOut ?? 0) * outputCostRate);
                decimal msgUsdRate = m.UsdRate ?? 25000m; // Fallback nếu tỷ giá null
                return msgCostUsd * msgUsdRate;
            });

            // 3. Tính lợi nhuận ròng
            dto.NetProfitVnd = dto.TotalRevenueVnd - dto.TotalCostVnd;

            // 4. Lấy danh sách 10 giao dịch gần nhất
            dto.RecentTransactions = await _context.Transactions
                .Include(t => t.User)
                .OrderByDescending(t => t.CreatedAt)
                .Take(10)
                .Select(t => new TransactionDto
                {
                    UserEmail = t.User != null ? t.User.Email : "Ẩn danh",
                    Amount = t.Amount,
                    Rate = t.UsdVndRate,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync();

            // 5. Thống kê Top Môn học hoạt động năng nổ nhất (dựa trên số câu hỏi của user)
            var subjectMetrics = await _context.ChatSessions
                .Include(s => s.Subject)
                .Select(s => new
                {
                    s.SubjectId,
                    SubjectCode = s.Subject != null ? s.Subject.Code : "N/A",
                    SubjectName = s.Subject != null ? s.Subject.Name : "N/A",
                    UserMsgCount = s.Messages.Count(m => m.Role == "user")
                })
                .ToListAsync();

            dto.TopSubjects = subjectMetrics
                .GroupBy(s => new { s.SubjectId, s.SubjectCode, s.SubjectName })
                .Select(g => new SubjectUsageDto
                {
                    SubjectId = g.Key.SubjectId,
                    SubjectCode = g.Key.SubjectCode,
                    SubjectName = g.Key.SubjectName,
                    QuestionCount = g.Sum(x => x.UserMsgCount)
                })
                .OrderByDescending(s => s.QuestionCount)
                .Take(5)
                .ToList();

            // 6. Thống kê Top Học liệu hữu ích được AI trích dẫn nhiều nhất
            var assistantMessages = await _context.ChatMessages
                .Where(m => m.Role == "assistant" && !string.IsNullOrEmpty(m.Citations) && m.Citations != "[]")
                .Select(m => m.Citations!)
                .ToListAsync();

            var citationCounts = new Dictionary<string, int>();
            foreach (var citationsJson in assistantMessages)
            {
                try
                {
                    var items = System.Text.Json.JsonSerializer.Deserialize<List<CitationItem>>(citationsJson, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (items != null)
                    {
                        foreach (var item in items)
                        {
                            if (!string.IsNullOrWhiteSpace(item.FileName))
                            {
                                if (citationCounts.ContainsKey(item.FileName))
                                    citationCounts[item.FileName]++;
                                else
                                    citationCounts[item.FileName] = 1;
                            }
                        }
                    }
                }
                catch
                {
                    // Tránh lỗi nát trang khi chuỗi json không hợp lệ
                }
            }

            dto.TopDocuments = citationCounts
                .OrderByDescending(kv => kv.Value)
                .Take(5)
                .Select(kv => new DocumentCitationDto
                {
                    DocumentName = kv.Key,
                    CitationCount = kv.Value
                })
                .ToList();

            // 7. Thống kê Phân loại Ý kiến góp ý học sinh (ContactMessages)
            var contactGroups = await _context.ContactMessages
                .GroupBy(c => c.Type)
                .Select(g => new
                {
                    Type = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            var totalFeedback = contactGroups.Sum(g => g.Count);
            dto.FeedbackStats = contactGroups.Select(g => new FeedbackTypeDto
            {
                TypeName = g.Type switch
                {
                    ContactType.DocumentIssue => "Lỗi tài liệu học liệu",
                    ContactType.ChatIssue => "Lỗi Chatbot (RAG)",
                    ContactType.GeneralFeedback => "Góp ý chung hệ thống",
                    _ => g.Type.ToString()
                },
                Count = g.Count,
                Percentage = totalFeedback > 0 ? (double)g.Count * 100.0 / totalFeedback : 0
            })
            .OrderByDescending(f => f.Count)
            .ToList();

            // 8. Tỷ lệ chuyển đổi Premium
            dto.TotalStudentsCount = await _context.AppUsers.CountAsync(u => u.Role == "Student");
            dto.PremiumStudentsCount = await _context.AppUsers.CountAsync(u => u.Role == "Student" && u.Subscription == AppUser.SubscriptionType.Premium);
            dto.PremiumConversionRate = dto.TotalStudentsCount > 0 ? (double)dto.PremiumStudentsCount * 100.0 / dto.TotalStudentsCount : 0.0;

            // 9. Biến động Token & Lượt chat theo ngày (7 ngày gần nhất)
            var sevenDaysAgo = DateTime.UtcNow.Date.AddDays(-6);
            var dailyUsageData = await _context.ChatMessages
                .Where(m => m.Role == "assistant" && m.Timestamp >= sevenDaysAgo && (m.TokenIn != null || m.TokenOut != null))
                .GroupBy(m => m.Timestamp.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    TotalIn = g.Sum(x => x.TokenIn ?? 0),
                    TotalOut = g.Sum(x => x.TokenOut ?? 0),
                    MsgCount = g.Count()
                })
                .ToListAsync();

            dto.DailyTrends = new List<DailyUsageDto>();
            for (int i = 6; i >= 0; i--)
            {
                var date = sevenDaysAgo.AddDays(i);
                var match = dailyUsageData.FirstOrDefault(d => d.Date == date);
                dto.DailyTrends.Add(new DailyUsageDto
                {
                    Date = date,
                    InputTokens = match?.TotalIn ?? 0,
                    OutputTokens = match?.TotalOut ?? 0,
                    MessageCount = match?.MsgCount ?? 0
                });
            }

            return dto;
        }

        public async Task<IEnumerable<AppUserDto>> GetTopTokenConsumersAsync(int topCount = 10)
        {
            var topStats = await _context.ChatMessages
                .Where(m => m.Session != null && m.Session.User != null)
                .GroupBy(m => new { m.Session.UserId, m.Session.User.Email, m.Session.User.FirstName, m.Session.User.LastName })
                .Select(g => new {
                    UserId = g.Key.UserId,
                    Email = g.Key.Email,
                    FirstName = g.Key.FirstName,
                    LastName = g.Key.LastName,
                    TokenIn = g.Sum(m => (long)(m.TokenIn ?? 0)),
                    TokenOut = g.Sum(m => (long)(m.TokenOut ?? 0)),
                    TotalTokens = g.Sum(m => (long)(m.TokenIn ?? 0)) + g.Sum(m => (long)(m.TokenOut ?? 0))
                })
                .OrderByDescending(x => x.TotalTokens)
                .Take(topCount)
                .ToListAsync();

            var priceConfig = await _settingService.GetPricingConfigAsync();
            decimal inRate = priceConfig.TokenInCostPerMillion / 1000000m;
            decimal outRate = priceConfig.TokenOutCostPerMillion / 1000000m;

            return topStats.Select(s => new AppUserDto
            {
                Id = s.UserId,
                Email = s.Email,
                FirstName = s.FirstName,
                LastName = s.LastName,
                TokenIn = s.TokenIn,
                TokenOut = s.TokenOut,
                CostVnd = (s.TokenIn * inRate + s.TokenOut * outRate) * priceConfig.UsdVndRate
            });
        }
    }
}
