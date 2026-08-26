using System.ComponentModel.DataAnnotations;
using static System.Net.Mime.MediaTypeNames;

namespace AltriumRecruitmentSystem.Models
{
    public enum UserRole
    {
        Applicant,
        Recruiter,
        Admin,
        Management 
    }

    public class User
    {
        public int UserId { get; set; }

        [Required, MaxLength(50)]
        public required string FirstName { get; set; }

        [Required, MaxLength(50)]
        public required string LastName { get; set; }

        [Required, MaxLength(100)]
        public required string Email { get; set; }

        [Required]
        public required string PasswordHash { get; set; }

        public UserRole Role { get; set; } = UserRole.Applicant;

        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? Country { get; set; }

        public DateTime RegisteredOn { get; set; } = DateTime.Now;

        // Navigation properties
        public ICollection<JobApplication> Applications { get; set; } = new List<JobApplication>();
        public ICollection<Education> EducationEntries { get; set; } = new List<Education>();
        public ICollection<Experience> ExperienceEntries { get; set; } = new List<Experience>();
        public string? CvFileName { get; set; }
        public string? CvFilePath { get; set; }
        public DateTime? CvUploadedOn { get; set; }

        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}