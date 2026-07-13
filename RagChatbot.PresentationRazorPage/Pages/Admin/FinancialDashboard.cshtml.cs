using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RagChatbot.Business.Interfaces;

namespace RagChatbot.PresentationRazorPage.Pages.Admin
{
    [Authorize(Roles = "Admin")] // Đảm bảo chỉ Admin mới vào được
    public class FinancialDashboardModel : PageModel
    {
        private readonly IFinancialService _financialService;

        public FinancialDashboardModel(IFinancialService financialService)
        {
            _financialService = financialService;
        }

        // Định nghĩa các chỉ số hiển thị ở View
        public decimal TotalRevenueVnd { get; set; }
        public long TotalInputTokens { get; set; }
        public long TotalOutputTokens { get; set; }
        public decimal TotalCostUsd { get; set; }
        public decimal TotalCostVnd { get; set; }
        public decimal NetProfitVnd { get; set; }
        public List<TransactionDto> RecentTransactions { get; set; } = new();

        // Các chỉ số thống kê vận hành mới
        public List<SubjectUsageDto> TopSubjects { get; set; } = new();
        public List<DocumentCitationDto> TopDocuments { get; set; } = new();
        public List<FeedbackTypeDto> FeedbackStats { get; set; } = new();
        public double PremiumConversionRate { get; set; }
        public int TotalStudentsCount { get; set; }
        public int PremiumStudentsCount { get; set; }
        public List<DailyUsageDto> DailyTrends { get; set; } = new();
        
        public List<RagChatbot.Business.DTOs.AppUserDto> TopTokenConsumers { get; set; } = new();

        public async Task OnGetAsync()
        {
            var data = await _financialService.GetDashboardDataAsync();

            TotalRevenueVnd = data.TotalRevenueVnd;
            TotalInputTokens = data.TotalInputTokens;
            TotalOutputTokens = data.TotalOutputTokens;
            TotalCostUsd = data.TotalCostUsd;
            TotalCostVnd = data.TotalCostVnd;
            NetProfitVnd = data.NetProfitVnd;

            // Map RecentTransactions
            RecentTransactions.Clear();
            foreach (var t in data.RecentTransactions)
            {
                RecentTransactions.Add(new TransactionDto
                {
                    UserEmail = t.UserEmail,
                    Amount = t.Amount,
                    Rate = t.Rate,
                    CreatedAt = t.CreatedAt
                });
            }

            // Map TopSubjects
            TopSubjects.Clear();
            foreach (var s in data.TopSubjects)
            {
                TopSubjects.Add(new SubjectUsageDto
                {
                    SubjectId = s.SubjectId,
                    SubjectCode = s.SubjectCode,
                    SubjectName = s.SubjectName,
                    QuestionCount = s.QuestionCount
                });
            }

            // Map TopDocuments
            TopDocuments.Clear();
            foreach (var d in data.TopDocuments)
            {
                TopDocuments.Add(new DocumentCitationDto
                {
                    DocumentName = d.DocumentName,
                    CitationCount = d.CitationCount
                });
            }

            // Map FeedbackStats
            FeedbackStats.Clear();
            foreach (var f in data.FeedbackStats)
            {
                FeedbackStats.Add(new FeedbackTypeDto
                {
                    TypeName = f.TypeName,
                    Count = f.Count,
                    Percentage = f.Percentage
                });
            }

            PremiumConversionRate = data.PremiumConversionRate;
            TotalStudentsCount = data.TotalStudentsCount;
            PremiumStudentsCount = data.PremiumStudentsCount;

            // Map DailyTrends
            DailyTrends.Clear();
            foreach (var trend in data.DailyTrends)
            {
                DailyTrends.Add(new DailyUsageDto
                {
                    Date = trend.Date,
                    InputTokens = trend.InputTokens,
                    OutputTokens = trend.OutputTokens,
                    MessageCount = trend.MessageCount
                });
            }

            var topConsumers = await _financialService.GetTopTokenConsumersAsync(10);
            TopTokenConsumers.AddRange(topConsumers);
        }
    }

    public class TransactionDto
    {
        public string UserEmail { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal Rate { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // Các DTO mới định nghĩa hỗ trợ thống kê vận hành
    public class SubjectUsageDto
    {
        public int SubjectId { get; set; }
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public int QuestionCount { get; set; }
    }

    public class DocumentCitationDto
    {
        public string DocumentName { get; set; } = string.Empty;
        public int CitationCount { get; set; }
    }

    public class FeedbackTypeDto
    {
        public string TypeName { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    public class DailyUsageDto
    {
        public DateTime Date { get; set; }
        public int InputTokens { get; set; }
        public int OutputTokens { get; set; }
        public int MessageCount { get; set; }
    }

    public class CitationItem
    {
        public string FileName { get; set; } = string.Empty;
        public int Page { get; set; }
    }
}