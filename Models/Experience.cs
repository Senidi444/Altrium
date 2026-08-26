namespace AltriumRecruitmentSystem.Models
{
    public class Experience
    {
        public int ExperienceId { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public required string Title { get; set; }
        public required string Company { get; set; }
        public required string Duration { get; set; }
    }
}