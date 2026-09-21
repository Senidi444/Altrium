namespace AltriumRecruitmentSystem.Models
{
    public enum NotificationType
    {
        ApplicationUpdate,
        InterviewInvite,
        InterviewUpdate,
        General,
        SystemAlert
    }

    public class Notification
    {
        public int NotificationId { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public required string Message { get; set; }

        public NotificationType NotificationType { get; set; } = NotificationType.General;

        public DateTime CreateDate { get; set; } = DateTime.Now;

        public bool IsRead { get; set; } = false;
    }
}