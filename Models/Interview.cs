namespace AltriumRecruitmentSystem.Models
{
    public enum InterviewStatus
    {
        Scheduled,
        Completed,
        Cancelled,
        NoShow
    }

    public class Interview
    {
        public int InterviewId { get; set; }

        public int ApplicationId { get; set; }
        public JobApplication Application { get; set; } = null!;

        public DateTime InterviewDate { get; set; }
        public TimeSpan InterviewTime { get; set; }

        public string? Stage { get; set; }
        public string? InterviewType { get; set; }
        public InterviewStatus Status { get; set; } = InterviewStatus.Scheduled;

        public string? GoogleMeetLink { get; set; }
        public string? Message { get; set; }

        public bool NotifyApplicant { get; set; } = true;

        public DateTime ScheduledOn { get; set; } = DateTime.Now;
    }
}