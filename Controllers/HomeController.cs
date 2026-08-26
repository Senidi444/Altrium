using AltriumRecruitmentSystem.Models;
using AltriumRecruitmentSystem.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Diagnostics;

namespace AltriumRecruitmentSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult AboutUs()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }

        // ===================== REGISTER =====================

        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            bool emailExists = await _context.Users.AnyAsync(u => u.Email == model.Email);
            if (emailExists)
            {
                ModelState.AddModelError("Email", "This email is already registered.");
                return View(model);
            }

            var user = new User
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                Role = UserRole.Applicant
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return RedirectToAction("Login");
        }

        // ===================== LOGIN =====================

        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

            return user.Role switch
            {
                UserRole.Recruiter => RedirectToAction("RecruiterDashboard"),
                UserRole.Admin => RedirectToAction("AdminDashboard"),
                UserRole.Management => RedirectToAction("ManagementDashboard"),
                _ => RedirectToAction("ApplicantDashboard")
            };
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        // ===================== APPLICANT DASHBOARD =====================

        [Authorize(Roles = "Applicant")]
        public IActionResult ApplicantDashboard()
        {
            return View();
        }

        // ===================== COMPLETE PROFILE =====================

        [Authorize(Roles = "Applicant")]
        public async Task<IActionResult> CompleteProfile()
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var user = await _context.Users
                .Include(u => u.EducationEntries)
                .Include(u => u.ExperienceEntries)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null) return NotFound();

            var viewModel = new CompleteProfilePageViewModel
            {
                PersonalInfo = new PersonalInfoViewModel
                {
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Phone = user.Phone,
                    Email = user.Email,
                    Address = user.Address,
                    Country = user.Country
                },
                Educations = user.EducationEntries.ToList(),
                Experiences = user.ExperienceEntries.ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        [Authorize(Roles = "Applicant")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveProfile(PersonalInfoViewModel model)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Phone = model.Phone;
            user.Address = model.Address;
            user.Country = model.Country;

            await _context.SaveChangesAsync();

            return RedirectToAction("CompleteProfile");
        }

        [HttpPost]
        [Authorize(Roles = "Applicant")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEducation(AddEducationViewModel model)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var education = new Education
            {
                UserId = userId,
                Title = model.Title,
                Institution = model.Institution,
                Year = model.Year
            };

            _context.Educations.Add(education);
            await _context.SaveChangesAsync();

            return RedirectToAction("CompleteProfile");
        }

        [HttpPost]
        [Authorize(Roles = "Applicant")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEducation(int id)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var education = await _context.Educations.FirstOrDefaultAsync(e => e.EducationId == id && e.UserId == userId);

            if (education != null)
            {
                _context.Educations.Remove(education);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("CompleteProfile");
        }

        [HttpPost]
        [Authorize(Roles = "Applicant")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddExperience(AddExperienceViewModel model)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var experience = new Experience
            {
                UserId = userId,
                Title = model.Title,
                Company = model.Company,
                Duration = model.Duration
            };

            _context.Experiences.Add(experience);
            await _context.SaveChangesAsync();

            return RedirectToAction("CompleteProfile");
        }

        [HttpPost]
        [Authorize(Roles = "Applicant")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteExperience(int id)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var experience = await _context.Experiences.FirstOrDefaultAsync(e => e.ExperienceId == id && e.UserId == userId);

            if (experience != null)
            {
                _context.Experiences.Remove(experience);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("CompleteProfile");
        }

        // ===================== UPLOAD CV =====================

        [Authorize(Roles = "Applicant")]
        public async Task<IActionResult> UploadCV()
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            return View(user);
        }

        [HttpPost]
        [Authorize(Roles = "Applicant")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadCV(CvUploadViewModel model)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            if (model.CvFile == null || model.CvFile.Length == 0)
            {
                TempData["UploadError"] = "Please choose a file to upload.";
                return RedirectToAction("UploadCV");
            }

            if (Path.GetExtension(model.CvFile.FileName).ToLower() != ".pdf")
            {
                TempData["UploadError"] = "Only PDF files are accepted.";
                return RedirectToAction("UploadCV");
            }

            if (model.CvFile.Length > 5 * 1024 * 1024)
            {
                TempData["UploadError"] = "File size must be under 5 MB.";
                return RedirectToAction("UploadCV");
            }

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "cvs");
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"user_{userId}_cv.pdf";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await model.CvFile.CopyToAsync(stream);
            }

            user.CvFileName = model.CvFile.FileName;
            user.CvFilePath = $"/uploads/cvs/{uniqueFileName}";
            user.CvUploadedOn = DateTime.Now;

            await _context.SaveChangesAsync();

            return RedirectToAction("UploadCV");
        }

        [HttpPost]
        [Authorize(Roles = "Applicant")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCV()
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            if (!string.IsNullOrEmpty(user.CvFilePath))
            {
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.CvFilePath.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }

            user.CvFileName = null;
            user.CvFilePath = null;
            user.CvUploadedOn = null;

            await _context.SaveChangesAsync();

            return RedirectToAction("UploadCV");
        }

        // ===================== BROWSE VACANCIES =====================

        [Authorize(Roles = "Applicant")]
        public async Task<IActionResult> BrowseVacancies(string? search, string? department, string? location)
        {
            var query = _context.Vacancies.Where(v => v.Status == VacancyStatus.Open).AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(v => v.JobTitle.Contains(search));
            }

            if (!string.IsNullOrEmpty(department) && department != "All Roles")
            {
                query = query.Where(v => v.Department == department);
            }

            if (!string.IsNullOrEmpty(location) && location != "All Locations")
            {
                query = query.Where(v => v.Location == location);
            }

            var vacancies = await query.OrderByDescending(v => v.PostedOn).ToListAsync();

            var viewModel = new BrowseVacanciesViewModel
            {
                Vacancies = vacancies,
                SearchTerm = search,
                DepartmentFilter = department,
                LocationFilter = location
            };

            return View(viewModel);
        }

        // ===================== VACANCY DETAILS + APPLY =====================

        [Authorize(Roles = "Applicant")]
        public async Task<IActionResult> VacancyDetails(int id)
        {
            var vacancy = await _context.Vacancies.FirstOrDefaultAsync(v => v.VacancyId == id);
            if (vacancy == null) return NotFound();

            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            bool alreadyApplied = await _context.Applications.AnyAsync(a => a.UserId == userId && a.VacancyId == id);

            var viewModel = new VacancyDetailsViewModel
            {
                Vacancy = vacancy,
                AlreadyApplied = alreadyApplied,
                JustApplied = TempData["JustApplied"] != null
            };

            return View(viewModel);
        }

        [HttpPost]
        [Authorize(Roles = "Applicant")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyForVacancy(int vacancyId)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            bool alreadyApplied = await _context.Applications.AnyAsync(a => a.UserId == userId && a.VacancyId == vacancyId);

            if (!alreadyApplied)
            {
                var application = new JobApplication
                {
                    UserId = userId,
                    VacancyId = vacancyId,
                    Status = ApplicationStatus.Applied
                };

                _context.Applications.Add(application);
                await _context.SaveChangesAsync();

                TempData["JustApplied"] = true;
            }

            return RedirectToAction("VacancyDetails", new { id = vacancyId });
        }

        // ===================== MY APPLICATIONS =====================

        [Authorize(Roles = "Applicant")]
        public async Task<IActionResult> MyApplications()
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var applications = await _context.Applications
                .Include(a => a.Vacancy)
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.AppliedOn)
                .ToListAsync();

            return View(applications);
        }

        // ===================== RECRUITER DASHBOARD =====================

        [Authorize(Roles = "Recruiter")]
        public async Task<IActionResult> RecruiterDashboard()
        {
            var recentApplications = await _context.Applications
                .Include(a => a.User)
                .Include(a => a.Vacancy)
                .OrderByDescending(a => a.AppliedOn)
                .Take(5)
                .ToListAsync();

            ViewBag.TotalVacancies = await _context.Vacancies.CountAsync();
            ViewBag.TotalApplicants = await _context.Applications.Select(a => a.UserId).Distinct().CountAsync();
            ViewBag.NewApplicants = await _context.Applications.CountAsync(a => a.Status == ApplicationStatus.Applied);
            ViewBag.ShortlistedCount = await _context.Applications.CountAsync(a => a.Status == ApplicationStatus.Shortlisted);

            return View(recentApplications);
        }

        // ===================== VACANCY MANAGEMENT =====================

        [Authorize(Roles = "Recruiter")]
        public async Task<IActionResult> VacancyManagement()
        {
            var vacancies = await _context.Vacancies
                .OrderByDescending(v => v.PostedOn)
                .ToListAsync();

            return View(vacancies);
        }

        [Authorize(Roles = "Recruiter")]
        public IActionResult CreateVacancy()
        {
            return View(new CreateVacancyViewModel());
        }

        [HttpPost]
        [Authorize(Roles = "Recruiter")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateVacancy(CreateVacancyViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var vacancy = new Vacancy
            {
                JobTitle = model.JobTitle,
                Department = model.Department,
                Location = model.Location,
                EmploymentType = model.EmploymentType,
                Description = model.Description,
                Requirements = model.Requirements,
                Status = VacancyStatus.Open
            };

            _context.Vacancies.Add(vacancy);
            await _context.SaveChangesAsync();

            return RedirectToAction("VacancyManagement");
        }

        [HttpPost]
        [Authorize(Roles = "Recruiter")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVacancy(int id)
        {
            var vacancy = await _context.Vacancies.FindAsync(id);
            if (vacancy != null)
            {
                _context.Vacancies.Remove(vacancy);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("VacancyManagement");
        }

        // ===================== APPLICANTS MANAGEMENT =====================

        [Authorize(Roles = "Recruiter")]
        public async Task<IActionResult> ApplicantsManagement(int? vacancyId, string? search)
        {
            var query = _context.Applications
                .Include(a => a.User)
                .Include(a => a.Vacancy)
                .AsQueryable();

            if (vacancyId.HasValue)
            {
                query = query.Where(a => a.VacancyId == vacancyId);
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(a => (a.User.FirstName + " " + a.User.LastName).Contains(search) || a.User.Email.Contains(search));
            }

            var applications = await query.OrderByDescending(a => a.AppliedOn).ToListAsync();
            var allVacancies = await _context.Vacancies.ToListAsync();

            var viewModel = new ApplicantsManagementViewModel
            {
                Applications = applications,
                AllVacancies = allVacancies,
                VacancyFilter = vacancyId,
                SearchTerm = search
            };

            return View(viewModel);
        }

        [Authorize(Roles = "Recruiter")]
        public async Task<IActionResult> ViewApplicantProfile(int userId)
        {
            var applicant = await _context.Users
                .Include(u => u.EducationEntries)
                .Include(u => u.ExperienceEntries)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (applicant == null) return NotFound();

            return View(applicant);
        }

        [HttpPost]
        [Authorize(Roles = "Recruiter")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ShortlistApplication(int id)
        {
            var application = await _context.Applications.FindAsync(id);
            if (application != null)
            {
                application.Status = ApplicationStatus.Shortlisted;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("ApplicantsManagement");
        }

        [HttpPost]
        [Authorize(Roles = "Recruiter")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectApplication(int id)
        {
            var application = await _context.Applications.FindAsync(id);
            if (application != null)
            {
                application.Status = ApplicationStatus.Rejected;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("ApplicantsManagement");
        }

        // ===================== SCHEDULE INTERVIEW =====================

        [Authorize(Roles = "Recruiter")]
        public async Task<IActionResult> ScheduleInterview()
        {
            var shortlisted = await _context.Applications
                .Include(a => a.User)
                .Include(a => a.Vacancy)
                .Where(a => a.Status == ApplicationStatus.Shortlisted)
                .ToListAsync();

            var viewModel = new ScheduleInterviewViewModel
            {
                ApplicationOptions = shortlisted
            };

            return View(viewModel);
        }

        [HttpPost]
        [Authorize(Roles = "Recruiter")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ScheduleInterview(ScheduleInterviewViewModel model)
        {
            var interview = new Interview
            {
                ApplicationId = model.ApplicationId,
                InterviewDate = model.InterviewDate,
                InterviewTime = model.InterviewTime,
                GoogleMeetLink = model.GoogleMeetLink,
                Message = model.Message,
                NotifyApplicant = model.NotifyApplicant
            };

            _context.Interviews.Add(interview);
            await _context.SaveChangesAsync();

            return RedirectToAction("ScheduleInterview");
        }

        // ===================== ADMIN DASHBOARD =====================

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminDashboard()
        {
            var recentUsers = await _context.Users
                .OrderByDescending(u => u.RegisteredOn)
                .Take(5)
                .ToListAsync();

            ViewBag.TotalUsers = await _context.Users.CountAsync();
            ViewBag.TotalRecruiters = await _context.Users.CountAsync(u => u.Role == UserRole.Recruiter);
            ViewBag.TotalApplicants = await _context.Users.CountAsync(u => u.Role == UserRole.Applicant);
            ViewBag.ActiveVacancies = await _context.Vacancies.CountAsync(v => v.Status == VacancyStatus.Open);

            return View(recentUsers);
        }

        // ===================== MANAGEMENT DASHBOARD =====================

        [Authorize(Roles = "Management")]
        public async Task<IActionResult> ManagementDashboard()
        {
            ViewBag.TotalVacancies = await _context.Vacancies.CountAsync();
            ViewBag.TotalApplications = await _context.Applications.CountAsync();
            ViewBag.ShortlistedCount = await _context.Applications.CountAsync(a => a.Status == ApplicationStatus.Shortlisted);
            ViewBag.InterviewsCount = await _context.Interviews.CountAsync();

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}