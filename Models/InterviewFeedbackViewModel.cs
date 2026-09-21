using System.ComponentModel.DataAnnotations;

namespace AltriumRecruitmentSystem.Models
{
    public class InterviewFeedbackViewModel
    {
        public int InterviewId { get; set; }

        public int ApplicationId { get; set; }

        public string ApplicantName { get; set; } = "";

        public string JobTitle { get; set; } = "";

        public DateTime InterviewDate { get; set; }

        public TimeSpan InterviewTime { get; set; }

        public string? GoogleMeetLink { get; set; }

        [Required(ErrorMessage = "Please select an interview outcome.")]
        public string? Outcome { get; set; }

        [Required(ErrorMessage = "Please enter interviewer feedback.")]
        [MinLength(10, ErrorMessage = "Feedback should contain at least 10 characters.")]
        public string? InterviewerFeedback { get; set; }

        public InterviewStatus Status { get; set; } =
            InterviewStatus.Scheduled;
    }
}