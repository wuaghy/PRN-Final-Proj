using System;
using System.Collections.Generic;

namespace RagChatbot.Business.DTOs
{
    public class FinancialDashboardDto
    {
        public decimal TotalRevenueVnd { get; set; }
        public long TotalInputTokens { get; set; }
        public long TotalOutputTokens { get; set; }
        public decimal TotalCostUsd { get; set; }
        public decimal TotalCostVnd { get; set; }
        public decimal NetProfitVnd { get; set; }
        public List<TransactionDto> RecentTransactions { get; set; } = new();
        public List<SubjectUsageDto> TopSubjects { get; set; } = new();
        public List<DocumentCitationDto> TopDocuments { get; set; } = new();
        public List<FeedbackTypeDto> FeedbackStats { get; set; } = new();
        public double PremiumConversionRate { get; set; }
        public int TotalStudentsCount { get; set; }
        public int PremiumStudentsCount { get; set; }
        public List<DailyUsageDto> DailyTrends { get; set; } = new();
    }

    public class TransactionDto
    {
        public string UserEmail { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal Rate { get; set; }
        public DateTime CreatedAt { get; set; }
    }

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
