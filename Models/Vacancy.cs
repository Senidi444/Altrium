using System.ComponentModel.DataAnnotations;

namespace AltriumRecruitmentSystem.Models
{
    public enum VacancyStatus
    {
        Draft,
        Open,
        Closed,
        Filled,
        Cancelled
    }

    public class Vacancy
    {
        public int VacancyId { get; set; }

        [Required, MaxLength(100)]
        public required string JobTitle { get; set; }

        [Required, MaxLength(50)]
        public required string Department { get; set; }

        [Required, MaxLength(50)]
        public required string Location { get; set; }

        public string EmploymentType { get; set; } = "Full-Time";

        [Required]
        public required string Description { get; set; }

        [Required]
        public required string Requirements { get; set; }

        public DateTime PostedOn { get; set; } = DateTime.Now;

        public VacancyStatus Status { get; set; } = VacancyStatus.Open;

        // Navigation
        public ICollection<JobApplication> Applications { get; set; } = new List<JobApplication>();
    }
}