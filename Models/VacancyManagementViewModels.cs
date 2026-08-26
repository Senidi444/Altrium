using System.ComponentModel.DataAnnotations;

namespace AltriumRecruitmentSystem.Models
{
    public class CreateVacancyViewModel
    {
        [Required, MaxLength(100)]
        public string JobTitle { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Department { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Location { get; set; } = string.Empty;

        public string EmploymentType { get; set; } = "Full-Time";

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        public string Requirements { get; set; } = string.Empty;
    }

    public class ApplicantsManagementViewModel
    {
        public List<JobApplication> Applications { get; set; } = new();
        public List<Vacancy> AllVacancies { get; set; } = new();
        public int? VacancyFilter { get; set; }
        public string? SearchTerm { get; set; }
    }

    public class ScheduleInterviewViewModel
    {
        [Required]
        public int ApplicationId { get; set; }

        [Required]
        public DateTime InterviewDate { get; set; } = DateTime.Now.AddDays(3);

        [Required]
        public TimeSpan InterviewTime { get; set; } = new TimeSpan(10, 0, 0);

        public string? GoogleMeetLink { get; set; }
        public string? Message { get; set; }
        public bool NotifyApplicant { get; set; } = true;

        public List<JobApplication> ApplicationOptions { get; set; } = new();
    }
}