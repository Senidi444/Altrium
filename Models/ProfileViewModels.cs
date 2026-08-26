using System.ComponentModel.DataAnnotations;

namespace AltriumRecruitmentSystem.Models
{
    public class PersonalInfoViewModel
    {
        [Required, MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        public string? Phone { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string? Address { get; set; }
        public string? Country { get; set; }
    }

    public class AddEducationViewModel
    {
        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Institution { get; set; } = string.Empty;

        [Required]
        public string Year { get; set; } = string.Empty;
    }

    public class AddExperienceViewModel
    {
        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Company { get; set; } = string.Empty;

        [Required]
        public string Duration { get; set; } = string.Empty;
    }

    public class CompleteProfilePageViewModel
    {
        public PersonalInfoViewModel PersonalInfo { get; set; } = new();
        public List<Education> Educations { get; set; } = new();
        public List<Experience> Experiences { get; set; } = new();
    }
}