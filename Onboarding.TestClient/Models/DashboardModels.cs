namespace Onboarding.TestClient.Models
{
    /// <summary>
    /// Modelos para el Dashboard
    /// </summary>
    
    public class DashboardProspectListResponse
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public List<ProspectSummary> Prospects { get; set; } = new();
    }

    public class ProspectSummary
    {
        public Guid ProspectId { get; set; }
        public Guid UserId { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public int WorkflowId { get; set; }
        public string WorkflowName { get; set; } = string.Empty;
        public string WorkflowType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int? CurrentStepId { get; set; }
        public string CurrentStepName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int DaysActive { get; set; }
    }

    public class DashboardStatisticsResponse
    {
        public StatisticsSummary Summary { get; set; } = new();
        public List<WorkflowStatistics> ByWorkflow { get; set; } = new();
        public List<StatusStatistics> ByStatus { get; set; } = new();
    }

    public class StatisticsSummary
    {
        public int TotalProspects { get; set; }
        public int ActiveProspects { get; set; }
        public int CompletedProspects { get; set; }
        public int RejectedProspects { get; set; }
        public int RecentProspects { get; set; }
        public double AverageCompletionDays { get; set; }
    }

    public class WorkflowStatistics
    {
        public int WorkflowId { get; set; }
        public string WorkflowName { get; set; } = string.Empty;
        public string WorkflowType { get; set; } = string.Empty;
        public int Count { get; set; }
        public int Active { get; set; }
        public int Completed { get; set; }
    }

    public class StatusStatistics
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    public class DashboardTimelineResponse
    {
        public int Days { get; set; }
        public int Limit { get; set; }
        public int Count { get; set; }
        public List<TimelineEvent> Timeline { get; set; } = new();
    }

    public class TimelineEvent
    {
        public Guid ProspectId { get; set; }
        public string EventType { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string WorkflowName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int? CurrentStepId { get; set; }
    }

    public class DashboardSearchResponse
    {
        public string Query { get; set; } = string.Empty;
        public int Count { get; set; }
        public List<SearchResult> Results { get; set; } = new();
    }

    public class SearchResult
    {
        public Guid ProspectId { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string WorkflowName { get; set; } = string.Empty;
        public string Status { get; set; }= string.Empty;
        public DateTime CreatedAt { get; set; }
        public string MatchType { get; set; } = string.Empty;
    }
}
