using Microsoft.AspNetCore.Http;

namespace AltriumRecruitmentSystem.Models
{
    public class CvUploadViewModel
    {
        public IFormFile? CvFile { get; set; }
    }
}