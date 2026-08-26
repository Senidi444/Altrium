namespace AltriumRecruitmentSystem.Models
{
    public class BrowseVacanciesViewModel
    {
        public List<Vacancy> Vacancies { get; set; } = new();
        public string? SearchTerm { get; set; }
        public string? DepartmentFilter { get; set; }
        public string? LocationFilter { get; set; }
    }

    public class VacancyDetailsViewModel
    {
        public Vacancy Vacancy { get; set; } = null!;
        public bool AlreadyApplied { get; set; }
        public bool JustApplied { get; set; }
    }
}