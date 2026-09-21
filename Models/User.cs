using System.ComponentModel.DataAnnotations;

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

        // ===================== ACCOUNT STATUS =====================

        // Controls whether the user is allowed to log in.
        // Existing users will be given true when the migration is created.
        public bool IsActive { get; set; } = true;

        // Used for recruiter approval.
        // Existing users will be given true so current accounts
        // continue working normally.
        public bool IsApproved { get; set; } = true;


        // ===================== PERSONAL INFORMATION =====================

        public string? Phone { get; set; }

        public string? Address { get; set; }

        public string? Country { get; set; }

        public DateTime RegisteredOn { get; set; } = DateTime.Now;


        // ===================== NAVIGATION PROPERTIES =====================

        public ICollection<JobApplication> Applications
        {
            get;
            set;
        } = new List<JobApplication>();

        public ICollection<Education> EducationEntries
        {
            get;
            set;
        } = new List<Education>();

        public ICollection<Experience> ExperienceEntries
        {
            get;
            set;
        } = new List<Experience>();


        // ===================== CV =====================

        public string? CvFileName { get; set; }

        public string? CvFilePath { get; set; }

        public DateTime? CvUploadedOn { get; set; }


        // ===================== NOTIFICATIONS =====================

        public ICollection<Notification> Notifications
        {
            get;
            set;
        } = new List<Notification>();
    }
}