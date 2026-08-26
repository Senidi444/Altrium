using System.ComponentModel.DataAnnotations;

namespace AltriumRecruitmentSystem.Models
{
    public enum ApplicationStatus
    {
        Applied,
        UnderReview,
        Shortlisted,
        Rejected
    }

    public class JobApplication
    {
        [Key]
        public int ApplicationId { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public int VacancyId { get; set; }
        public Vacancy Vacancy { get; set; } = null!;

        public DateTime AppliedOn { get; set; } = DateTime.Now;

        public ApplicationStatus Status { get; set; } = ApplicationStatus.Applied;

        // Navigation
        public Interview? Interview { get; set; }
    }
}