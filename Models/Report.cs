namespace AltriumRecruitmentSystem.Models
{
    public class Report
    {
        public int ReportId { get; set; }

        public required string ReportType { get; set; }

        public DateTime GenerateDate { get; set; } = DateTime.Now;

        public required string ReportDate { get; set; }
    }
}