namespace AltriumRecruitmentSystem.Models
{
    public class Education
    {
        public int EducationId { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public required string Title { get; set; }
        public required string Institution { get; set; }
        public required string Year { get; set; }
    }
}