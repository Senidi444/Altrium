using AltriumRecruitmentSystem.Models;
using AltriumRecruitmentSystem.Data;
using AltriumRecruitmentSystem.Services;
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
        private readonly EmailService _emailService;

        public HomeController(
            AppDbContext context,
            EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // ===================== BASIC PAGES =====================

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

            bool emailExists = await _context.Users
                .AnyAsync(u => u.Email == model.Email);

            if (emailExists)
            {
                ModelState.AddModelError(
                    "Email",
                    "This email is already registered."
                );

                return View(model);
            }

            var user = new User
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                Role = UserRole.Applicant,
                IsActive = true,
                IsApproved = true
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

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user == null ||
                !BCrypt.Net.BCrypt.Verify(
                    model.Password,
                    user.PasswordHash))
            {
                ModelState.AddModelError(
                    "",
                    "Invalid email or password."
                );

                return View(model);
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError(
                    "",
                    "Your account has been deactivated. Please contact an administrator."
                );

                return View(model);
            }

            if (user.Role == UserRole.Recruiter &&
                !user.IsApproved)
            {
                ModelState.AddModelError(
                    "",
                    "Your recruiter account is awaiting administrator approval."
                );

                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    user.UserId.ToString()
                ),

                new Claim(
                    ClaimTypes.Name,
                    $"{user.FirstName} {user.LastName}"
                ),

                new Claim(
                    ClaimTypes.Email,
                    user.Email
                ),

                new Claim(
                    ClaimTypes.Role,
                    user.Role.ToString()
                )
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8),
                AllowRefresh = true
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                authProperties
            );

            return user.Role switch
            {
                UserRole.Recruiter =>
                    RedirectToAction("RecruiterDashboard"),

                UserRole.Admin =>
                    RedirectToAction("AdminDashboard"),

                UserRole.Management =>
                    RedirectToAction("ManagementDashboard"),

                _ =>
                    RedirectToAction("ApplicantDashboard")
            };
        }


        // ===================== LOGOUT =====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            return RedirectToAction("Login");
        }


        // ===================== APPLICANT DASHBOARD =====================

        [Authorize(Roles = "Applicant")]
        public async Task<IActionResult> ApplicantDashboard()
        {
            int userId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value
            );

            var applications = await _context.Applications
                .Include(a => a.Vacancy)
                .Include(a => a.Interview)
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.AppliedOn)
                .Take(5)
                .ToListAsync();

            ViewBag.TotalApplications =
                await _context.Applications
                    .CountAsync(a => a.UserId == userId);

            ViewBag.UnderReviewCount =
                await _context.Applications
                    .CountAsync(a =>
                        a.UserId == userId &&
                        a.Status == ApplicationStatus.UnderReview);

            ViewBag.ShortlistedCount =
                await _context.Applications
                    .CountAsync(a =>
                        a.UserId == userId &&
                        a.Status == ApplicationStatus.Shortlisted);

            ViewBag.RejectedCount =
                await _context.Applications
                    .CountAsync(a =>
                        a.UserId == userId &&
                        a.Status == ApplicationStatus.Rejected);

            ViewBag.UnreadNotifications =
                await _context.Notifications
                    .CountAsync(n =>
                        n.UserId == userId &&
                        !n.IsRead);

            return View(applications);
        }


        // ===================== COMPLETE PROFILE =====================

        private async Task<CompleteProfilePageViewModel?>
            BuildCompleteProfileViewModelAsync(int userId)
        {
            var user = await _context.Users
                .Include(u => u.EducationEntries)
                .Include(u => u.ExperienceEntries)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                return null;
            }

            return new CompleteProfilePageViewModel
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
        }


        [Authorize(Roles = "Applicant")]
        public async Task<IActionResult> CompleteProfile()
        {
            int userId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value
            );

            var viewModel =
                await BuildCompleteProfileViewModelAsync(userId);

            if (viewModel == null)
            {
                return NotFound();
            }

            if (TempData["ApplyError"] != null)
            {
                ViewBag.ApplyError = TempData["ApplyError"];
            }

            return View(viewModel);
        }


        // ===================== SAVE PROFILE =====================

        [HttpPost]
        [Authorize(Roles = "Applicant")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveProfile(
            PersonalInfoViewModel model)
        {
            int userId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value
            );

            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Phone = model.Phone;
            user.Address = model.Address;
            user.Country = model.Country;

            await _context.SaveChangesAsync();

            return RedirectToAction("CompleteProfile");
        }


        // ===================== EDUCATION =====================

        [HttpPost]
        [Authorize(Roles = "Applicant")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEducation(
            AddEducationViewModel model)
        {
            int userId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value
            );

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
            int userId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value
            );

            var education = await _context.Educations
                .FirstOrDefaultAsync(e =>
                    e.EducationId == id &&
                    e.UserId == userId);

            if (education != null)
            {
                _context.Educations.Remove(education);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction("CompleteProfile");
        }


        // ===================== EXPERIENCE =====================

        [HttpPost]
        [Authorize(Roles = "Applicant")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddExperience(
            AddExperienceViewModel model)
        {
            int userId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value
            );

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
            int userId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value
            );

            var experience = await _context.Experiences
                .FirstOrDefaultAsync(e =>
                    e.ExperienceId == id &&
                    e.UserId == userId);

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
            int userId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value
            );

            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }


        [HttpPost]
        [Authorize(Roles = "Applicant")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadCV(
            CvUploadViewModel model)
        {
            int userId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value
            );

            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            if (model.CvFile == null ||
                model.CvFile.Length == 0)
            {
                TempData["UploadError"] =
                    "Please choose a file to upload.";

                return RedirectToAction("UploadCV");
            }

            if (Path.GetExtension(
                    model.CvFile.FileName)
                    .ToLower() != ".pdf")
            {
                TempData["UploadError"] =
                    "Only PDF files are accepted.";

                return RedirectToAction("UploadCV");
            }

            if (model.CvFile.Length > 5 * 1024 * 1024)
            {
                TempData["UploadError"] =
                    "File size must be under 5 MB.";

                return RedirectToAction("UploadCV");
            }

            var uploadsFolder = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "uploads",
                "cvs"
            );

            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName =
                $"user_{userId}_cv.pdf";

            var filePath =
                Path.Combine(
                    uploadsFolder,
                    uniqueFileName
                );

            using (var stream =
                new FileStream(
                    filePath,
                    FileMode.Create))
            {
                await model.CvFile.CopyToAsync(stream);
            }

            user.CvFileName =
                model.CvFile.FileName;

            user.CvFilePath =
                $"/uploads/cvs/{uniqueFileName}";

            user.CvUploadedOn =
                DateTime.Now;

            await _context.SaveChangesAsync();

            return RedirectToAction("UploadCV");
        }


        [HttpPost]
        [Authorize(Roles = "Applicant")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCV()
        {
            int userId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value
            );

            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(user.CvFilePath))
            {
                var fullPath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    user.CvFilePath.TrimStart('/')
                );

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
        public async Task<IActionResult> BrowseVacancies(
            string? search,
            string? department,
            string? location)
        {
            var query = _context.Vacancies
                .Where(v => v.Status == VacancyStatus.Open)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(
                    v => v.JobTitle.Contains(search)
                );
            }

            if (!string.IsNullOrEmpty(department) &&
                department != "All Roles")
            {
                query = query.Where(
                    v => v.Department == department
                );
            }

            if (!string.IsNullOrEmpty(location) &&
                location != "All Locations")
            {
                query = query.Where(
                    v => v.Location == location
                );
            }

            var vacancies = await query
                .OrderByDescending(v => v.PostedOn)
                .ToListAsync();

            var viewModel = new BrowseVacanciesViewModel
            {
                Vacancies = vacancies,
                SearchTerm = search,
                DepartmentFilter = department,
                LocationFilter = location
            };

            return View(viewModel);
        }


        // ===================== VACANCY DETAILS =====================

        [Authorize(Roles = "Applicant")]
        public async Task<IActionResult> VacancyDetails(int id)
        {
            var vacancy = await _context.Vacancies
                .FirstOrDefaultAsync(v => v.VacancyId == id);

            if (vacancy == null)
            {
                return NotFound();
            }

            int userId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value
            );

            bool alreadyApplied =
                await _context.Applications.AnyAsync(
                    a =>
                        a.UserId == userId &&
                        a.VacancyId == id
                );

            var viewModel = new VacancyDetailsViewModel
            {
                Vacancy = vacancy,
                AlreadyApplied = alreadyApplied,
                JustApplied =
                    TempData["JustApplied"] != null,
                CanApply =
                    vacancy.Status == VacancyStatus.Open
            };

            return View(viewModel);
        }


        // ===================== APPLY FOR VACANCY =====================

        [HttpPost]
        [Authorize(Roles = "Applicant")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyForVacancy(
            int vacancyId)
        {
            int userId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value
            );

            var vacancy = await _context.Vacancies
                .FirstOrDefaultAsync(
                    v => v.VacancyId == vacancyId
                );

            if (vacancy == null)
            {
                return NotFound();
            }

            if (vacancy.Status != VacancyStatus.Open)
            {
                TempData["ApplyError"] =
                    "This vacancy is no longer available for applications.";

                return RedirectToAction(
                    "VacancyDetails",
                    new { id = vacancyId }
                );
            }

            var user = await _context.Users
                .Include(u => u.EducationEntries)
                .Include(u => u.ExperienceEntries)
                .FirstOrDefaultAsync(
                    u => u.UserId == userId
                );

            if (user == null)
            {
                return NotFound();
            }

            bool profileComplete =
                !string.IsNullOrEmpty(user.Phone) &&
                !string.IsNullOrEmpty(user.Address) &&
                !string.IsNullOrEmpty(user.Country) &&
                user.EducationEntries.Any() &&
                user.ExperienceEntries.Any() &&
                !string.IsNullOrEmpty(user.CvFilePath);

            if (!profileComplete)
            {
                TempData["ApplyError"] =
                    "Please complete your profile, add education, add experience, and upload your CV before applying.";

                return RedirectToAction(
                    "CompleteProfile"
                );
            }

            bool alreadyApplied =
                await _context.Applications.AnyAsync(
                    a =>
                        a.UserId == userId &&
                        a.VacancyId == vacancyId
                );

            if (alreadyApplied)
            {
                TempData["ApplyError"] =
                    "You have already applied for this vacancy.";

                return RedirectToAction(
                    "VacancyDetails",
                    new { id = vacancyId }
                );
            }

            var application = new JobApplication
            {
                UserId = userId,
                VacancyId = vacancyId,
                Status = ApplicationStatus.Applied
            };

            _context.Applications.Add(application);

            await _context.SaveChangesAsync();

            TempData["JustApplied"] = true;

            return RedirectToAction(
                "VacancyDetails",
                new { id = vacancyId }
            );
        }


        // ===================== MY APPLICATIONS =====================

        [Authorize(Roles = "Applicant")]
        public async Task<IActionResult> MyApplications()
        {
            int userId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value
            );

            var applications = await _context.Applications
                .Include(a => a.Vacancy)
                .Include(a => a.Interview)
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.AppliedOn)
                .ToListAsync();

            ViewBag.UnreadNotifications =
                await _context.Notifications
                    .CountAsync(n =>
                        n.UserId == userId &&
                        !n.IsRead);

            return View(applications);
        }


        // ===================== RECRUITER DASHBOARD =====================

        [Authorize(Roles = "Recruiter")]
        public async Task<IActionResult> RecruiterDashboard()
        {
            var recentApplications =
                await _context.Applications
                    .Include(a => a.User)
                    .Include(a => a.Vacancy)
                    .Include(a => a.Interview)
                    .OrderByDescending(a => a.AppliedOn)
                    .Take(5)
                    .ToListAsync();

            ViewBag.TotalVacancies =
                await _context.Vacancies.CountAsync();

            ViewBag.TotalApplicants =
                await _context.Applications
                    .Select(a => a.UserId)
                    .Distinct()
                    .CountAsync();

            ViewBag.NewApplicants =
                await _context.Applications
                    .CountAsync(
                        a => a.Status ==
                             ApplicationStatus.Applied
                    );

            ViewBag.ShortlistedCount =
                await _context.Applications
                    .CountAsync(
                        a => a.Status ==
                             ApplicationStatus.Shortlisted
                    );

            return View(recentApplications);
        }


        // ===================== VACANCY MANAGEMENT =====================

        [Authorize(Roles = "Recruiter")]
        public async Task<IActionResult> VacancyManagement()
        {
            var vacancies = await _context.Vacancies
                .Include(v => v.Applications)
                .AsNoTracking()
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
        public async Task<IActionResult> CreateVacancy(
            CreateVacancyViewModel model)
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

            return RedirectToAction(
                "VacancyManagement"
            );
        }


        // ===================== CLOSE VACANCY =====================

        [HttpPost]
        [Authorize(Roles = "Recruiter")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CloseVacancy(int id)
        {
            var vacancy = await _context.Vacancies
                .FirstOrDefaultAsync(v => v.VacancyId == id);

            if (vacancy == null)
            {
                TempData["VacancyError"] =
                    "Vacancy not found.";

                return RedirectToAction(
                    "VacancyManagement"
                );
            }

            if (vacancy.Status != VacancyStatus.Open)
            {
                TempData["VacancyError"] =
                    "This vacancy is already closed.";

                return RedirectToAction(
                    "VacancyManagement"
                );
            }

            vacancy.Status = VacancyStatus.Cancelled;

            await _context.SaveChangesAsync();

            TempData["VacancyMessage"] =
                "The vacancy has been closed. Existing recruitment history has been preserved.";

            return RedirectToAction(
                "VacancyManagement"
            );
        }


        // ===================== REOPEN VACANCY =====================

        [HttpPost]
        [Authorize(Roles = "Recruiter")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReopenVacancy(int id)
        {
            var vacancy = await _context.Vacancies
                .FirstOrDefaultAsync(v => v.VacancyId == id);

            if (vacancy == null)
            {
                TempData["VacancyError"] =
                    "Vacancy not found.";

                return RedirectToAction(
                    "VacancyManagement"
                );
            }

            if (vacancy.Status == VacancyStatus.Open)
            {
                TempData["VacancyError"] =
                    "This vacancy is already open.";

                return RedirectToAction(
                    "VacancyManagement"
                );
            }

            vacancy.Status = VacancyStatus.Open;

            await _context.SaveChangesAsync();

            TempData["VacancyMessage"] =
                "The vacancy has been reopened and is now available for applications.";

            return RedirectToAction(
                "VacancyManagement"
            );
        }


        // ===================== DELETE VACANCY =====================

        [HttpPost]
        [Authorize(Roles = "Recruiter")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVacancy(int id)
        {
            var vacancy = await _context.Vacancies
                .FindAsync(id);

            if (vacancy == null)
            {
                return RedirectToAction(
                    "VacancyManagement"
                );
            }

            bool hasRecruitmentData =
                await _context.Applications
                    .AnyAsync(
                        application =>
                            application.VacancyId == id
                    );

            if (hasRecruitmentData)
            {
                vacancy.Status =
                    VacancyStatus.Cancelled;

                TempData["VacancyMessage"] =
                    "The vacancy was cancelled. Its application and interview history has been preserved.";
            }
            else
            {
                _context.Vacancies.Remove(vacancy);

                TempData["VacancyMessage"] =
                    "The vacancy was deleted because it has no recruitment records.";
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(
                "VacancyManagement"
            );
        }


        // ===================== APPLICANTS MANAGEMENT =====================

        [Authorize(Roles = "Recruiter")]
        public async Task<IActionResult> ApplicantsManagement(
            int? vacancyId,
            string? search)
        {
            var query = _context.Applications
                .Include(a => a.User)
                .Include(a => a.Vacancy)
                .Include(a => a.Interview)
                .AsQueryable();

            if (vacancyId.HasValue)
            {
                query = query.Where(
                    a => a.VacancyId == vacancyId
                );
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(
                    a =>
                        (
                            a.User.FirstName +
                            " " +
                            a.User.LastName
                        ).Contains(search)
                        ||
                        a.User.Email.Contains(search)
                );
            }

            var applications = await query
                .OrderByDescending(a => a.AppliedOn)
                .ToListAsync();

            var allVacancies =
                await _context.Vacancies.ToListAsync();

            var viewModel =
                new ApplicantsManagementViewModel
                {
                    Applications = applications,
                    AllVacancies = allVacancies,
                    VacancyFilter = vacancyId,
                    SearchTerm = search
                };

            return View(viewModel);
        }


        [Authorize(Roles = "Recruiter")]
        public async Task<IActionResult> ViewApplicantProfile(
            int userId)
        {
            var applicant = await _context.Users
                .Include(u => u.EducationEntries)
                .Include(u => u.ExperienceEntries)
                .FirstOrDefaultAsync(
                    u => u.UserId == userId
                );

            if (applicant == null)
            {
                return NotFound();
            }

            return View(applicant);
        }


        // ===================== SHORTLIST =====================

        [HttpPost]
        [Authorize(Roles = "Recruiter")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ShortlistApplication(
            int id)
        {
            var application = await _context.Applications
                .Include(a => a.User)
                .Include(a => a.Vacancy)
                .FirstOrDefaultAsync(
                    a => a.ApplicationId == id
                );

            if (application != null)
            {
                application.Status =
                    ApplicationStatus.Shortlisted;

                var notification = new Notification
                {
                    UserId = application.UserId,

                    Message =
                        $"Your application for '{application.Vacancy.JobTitle}' has been shortlisted.",

                    NotificationType =
                        NotificationType.ApplicationUpdate,

                    CreateDate = DateTime.Now,

                    IsRead = false
                };

                _context.Notifications.Add(notification);

                await _context.SaveChangesAsync();

                // ===================== EMAIL =====================

                await _emailService.SendEmailAsync(
                    application.User.Email,
                    "Application Shortlisted - Altrium",
                    $"""
                    Dear {application.User.FirstName},

                    We are pleased to inform you that your application for the position of "{application.Vacancy.JobTitle}" has been shortlisted.

                    Please log in to your Altrium account to view your application status and any further updates.

                    Regards,
                    Altrium Recruitment System
                    """
                );
            }

            return RedirectToAction(
                "ApplicantsManagement"
            );
        }


        // ===================== REJECT =====================

        [HttpPost]
        [Authorize(Roles = "Recruiter")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectApplication(
            int id)
        {
            var application = await _context.Applications
                .Include(a => a.User)
                .Include(a => a.Vacancy)
                .FirstOrDefaultAsync(
                    a => a.ApplicationId == id
                );

            if (application != null)
            {
                application.Status =
                    ApplicationStatus.Rejected;

                var notification = new Notification
                {
                    UserId = application.UserId,

                    Message =
                        $"Your application for '{application.Vacancy.JobTitle}' has been rejected.",

                    NotificationType =
                        NotificationType.ApplicationUpdate,

                    CreateDate = DateTime.Now,

                    IsRead = false
                };

                _context.Notifications.Add(notification);

                await _context.SaveChangesAsync();

                // ===================== EMAIL =====================

                await _emailService.SendEmailAsync(
                    application.User.Email,
                    "Application Update - Altrium",
                    $"""
                    Dear {application.User.FirstName},

                    Thank you for your application for the position of "{application.Vacancy.JobTitle}".

                    We regret to inform you that your application has not been successful at this stage.

                    We appreciate your interest in Altrium and encourage you to check the platform for future opportunities.

                    Regards,
                    Altrium Recruitment System
                    """
                );
            }

            return RedirectToAction(
                "ApplicantsManagement"
            );
        }


        // ===================== SCHEDULE INTERVIEW =====================

        [Authorize(Roles = "Recruiter")]
        public async Task<IActionResult> ScheduleInterview()
        {
            var shortlisted =
                await _context.Applications
                    .Include(a => a.User)
                    .Include(a => a.Vacancy)
                    .Include(a => a.Interview)
                    .Where(
                        a =>
                            a.Status ==
                            ApplicationStatus.Shortlisted
                    )
                    .ToListAsync();

            var viewModel =
                new ScheduleInterviewViewModel
                {
                    ApplicationOptions =
                        shortlisted
                };

            return View(viewModel);
        }


        [HttpPost]
        [Authorize(Roles = "Recruiter")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ScheduleInterview(
            ScheduleInterviewViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var shortlisted =
                    await _context.Applications
                        .Include(a => a.User)
                        .Include(a => a.Vacancy)
                        .Include(a => a.Interview)
                        .Where(
                            a =>
                                a.Status ==
                                ApplicationStatus.Shortlisted
                        )
                        .ToListAsync();

                model.ApplicationOptions =
                    shortlisted;

                return View(model);
            }

            // ===================== GOOGLE MEET VALIDATION =====================

            if (string.IsNullOrWhiteSpace(model.GoogleMeetLink))
            {
                ModelState.AddModelError(
                    "GoogleMeetLink",
                    "Please enter a Google Meet link."
                );
            }
            else
            {
                bool validGoogleMeetLink =
                    Uri.TryCreate(
                        model.GoogleMeetLink.Trim(),
                        UriKind.Absolute,
                        out var meetUri
                    )
                    &&
                    meetUri.Scheme.Equals(
                        Uri.UriSchemeHttps,
                        StringComparison.OrdinalIgnoreCase
                    )
                    &&
                    (
                        meetUri.Host.Equals(
                            "meet.google.com",
                            StringComparison.OrdinalIgnoreCase
                        )
                        ||
                        meetUri.Host.Equals(
                            "www.meet.google.com",
                            StringComparison.OrdinalIgnoreCase
                        )
                    );

                if (!validGoogleMeetLink)
                {
                    ModelState.AddModelError(
                        "GoogleMeetLink",
                        "Please enter a valid Google Meet link, for example https://meet.google.com/abc-defg-hij."
                    );
                }
                else
                {
                    model.GoogleMeetLink =
                        model.GoogleMeetLink.Trim();
                }
            }

            if (!ModelState.IsValid)
            {
                var shortlisted =
                    await _context.Applications
                        .Include(a => a.User)
                        .Include(a => a.Vacancy)
                        .Include(a => a.Interview)
                        .Where(
                            a =>
                                a.Status ==
                                ApplicationStatus.Shortlisted
                        )
                        .ToListAsync();

                model.ApplicationOptions =
                    shortlisted;

                return View(model);
            }

            var application =
                await _context.Applications
                    .Include(a => a.User)
                    .Include(a => a.Vacancy)
                    .Include(a => a.Interview)
                    .FirstOrDefaultAsync(
                        a =>
                            a.ApplicationId ==
                            model.ApplicationId
                    );

            if (application == null)
            {
                TempData["InterviewError"] =
                    "The selected application could not be found.";

                return RedirectToAction(
                    "ScheduleInterview"
                );
            }

            if (application.Status !=
                ApplicationStatus.Shortlisted)
            {
                TempData["InterviewError"] =
                    "Only shortlisted applicants can be scheduled for an interview.";

                return RedirectToAction(
                    "ScheduleInterview"
                );
            }

            if (application.Interview != null)
            {
                TempData["InterviewError"] =
                    "An interview has already been scheduled for this application.";

                return RedirectToAction(
                    "ScheduleInterview"
                );
            }

            if (model.InterviewDate.Date < DateTime.Today)
            {
                ModelState.AddModelError(
                    "InterviewDate",
                    "Interview date cannot be in the past."
                );

                var shortlisted =
                    await _context.Applications
                        .Include(a => a.User)
                        .Include(a => a.Vacancy)
                        .Include(a => a.Interview)
                        .Where(
                            a =>
                                a.Status ==
                                ApplicationStatus.Shortlisted
                        )
                        .ToListAsync();

                model.ApplicationOptions =
                    shortlisted;

                return View(model);
            }

            var interview = new Interview
            {
                ApplicationId =
                    model.ApplicationId,

                InterviewDate =
                    model.InterviewDate,

                InterviewTime =
                    model.InterviewTime,

                GoogleMeetLink =
                    model.GoogleMeetLink,

                Message =
                    model.Message,

                NotifyApplicant =
                    model.NotifyApplicant
            };

            _context.Interviews.Add(interview);

            await _context.SaveChangesAsync();

            var notification = new Notification
            {
                UserId = application.UserId,

                Message =
                    $"An interview has been scheduled for your application for '{application.Vacancy.JobTitle}' on {model.InterviewDate:dd MMM yyyy} at {model.InterviewTime}.",

                NotificationType =
                    NotificationType.InterviewInvite,

                CreateDate = DateTime.Now,

                IsRead = false
            };

            _context.Notifications.Add(notification);

            await _context.SaveChangesAsync();

            // ===================== EMAIL =====================

            if (model.NotifyApplicant)
            {
                await _emailService.SendEmailAsync(
                    application.User.Email,
                    "Interview Scheduled - Altrium",
                    $"""
                    Dear {application.User.FirstName},

                    An interview has been scheduled for your application for the position of "{application.Vacancy.JobTitle}".

                    Interview Date: {model.InterviewDate:dd MMM yyyy}
                    Interview Time: {model.InterviewTime}

                    Google Meet Link:
                    {model.GoogleMeetLink}

                    Message from the recruiter:
                    {model.Message}

                    Please log in to your Altrium account for further details.

                    Regards,
                    Altrium Recruitment System
                    """
                );
            }

            TempData["InterviewMessage"] =
                "Interview scheduled successfully with a valid Google Meet link.";

            return RedirectToAction(
                "ScheduleInterview"
            );
        }


        // ===================== INTERVIEW FEEDBACK =====================

        [Authorize(Roles = "Recruiter")]
        public async Task<IActionResult> InterviewFeedback(int id)
        {
            var interview = await _context.Interviews
                .Include(i => i.Application)
                    .ThenInclude(a => a.User)
                .Include(i => i.Application)
                    .ThenInclude(a => a.Vacancy)
                .FirstOrDefaultAsync(
                    i => i.InterviewId == id
                );

            if (interview == null)
            {
                return NotFound();
            }

            if (interview.Status == InterviewStatus.Cancelled)
            {
                TempData["InterviewError"] =
                    "Cancelled interviews cannot receive feedback.";

                return RedirectToAction(
                    "ScheduleInterview"
                );
            }

            if (interview.Status == InterviewStatus.NoShow)
            {
                TempData["InterviewError"] =
                    "No-show interviews cannot receive feedback.";

                return RedirectToAction(
                    "ScheduleInterview"
                );
            }

            var viewModel = new InterviewFeedbackViewModel
            {
                InterviewId = interview.InterviewId,

                ApplicationId = interview.ApplicationId,

                ApplicantName =
                    $"{interview.Application.User.FirstName} {interview.Application.User.LastName}",

                JobTitle =
                    interview.Application.Vacancy.JobTitle,

                InterviewDate =
                    interview.InterviewDate,

                InterviewTime =
                    interview.InterviewTime,

                GoogleMeetLink =
                    interview.GoogleMeetLink,

                Outcome =
                    interview.Outcome,

                InterviewerFeedback =
                    interview.InterviewerFeedback,

                Status =
                    interview.Status
            };

            return View(viewModel);
        }


        [HttpPost]
        [Authorize(Roles = "Recruiter")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InterviewFeedback(
            InterviewFeedbackViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var interview = await _context.Interviews
                .Include(i => i.Application)
                    .ThenInclude(a => a.User)
                .Include(i => i.Application)
                    .ThenInclude(a => a.Vacancy)
                .FirstOrDefaultAsync(
                    i => i.InterviewId == model.InterviewId
                );

            if (interview == null)
            {
                return NotFound();
            }

            if (interview.Status == InterviewStatus.Cancelled)
            {
                ModelState.AddModelError(
                    "",
                    "Cancelled interviews cannot receive feedback."
                );

                return View(model);
            }

            if (interview.Status == InterviewStatus.NoShow)
            {
                ModelState.AddModelError(
                    "",
                    "No-show interviews cannot receive feedback."
                );

                return View(model);
            }

            interview.Outcome =
                model.Outcome;

            interview.InterviewerFeedback =
                model.InterviewerFeedback;

            interview.FeedbackSubmittedOn =
                DateTime.Now;

            interview.Status =
                InterviewStatus.Completed;

            var notification = new Notification
            {
                UserId =
                    interview.Application.UserId,

                Message =
                    $"Interview feedback has been recorded for your '{interview.Application.Vacancy.JobTitle}' interview. Outcome: {model.Outcome}.",

                NotificationType =
                    NotificationType.InterviewUpdate,

                CreateDate =
                    DateTime.Now,

                IsRead =
                    false
            };

            _context.Notifications.Add(notification);

            await _context.SaveChangesAsync();

            // ===================== EMAIL =====================

            await _emailService.SendEmailAsync(
                interview.Application.User.Email,
                "Interview Feedback - Altrium",
                $"""
                Dear {interview.Application.User.FirstName},

                Interview feedback has been recorded for your interview for the position of "{interview.Application.Vacancy.JobTitle}".

                Interview Outcome:
                {model.Outcome}

                Interviewer Feedback:
                {model.InterviewerFeedback}

                Please log in to your Altrium account to view your application and interview information.

                Regards,
                Altrium Recruitment System
                """
            );

            TempData["InterviewMessage"] =
                "Interview feedback has been saved successfully.";

            return RedirectToAction(
                "ScheduleInterview"
            );
        }


        // ===================== NOTIFICATIONS =====================

        [Authorize(Roles = "Applicant")]
        public async Task<IActionResult> Notifications()
        {
            int userId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value
            );

            var notifications =
                await _context.Notifications
                    .Where(n => n.UserId == userId)
                    .OrderByDescending(n => n.CreateDate)
                    .ToListAsync();

            return View(notifications);
        }


        [HttpPost]
        [Authorize(Roles = "Applicant")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkNotificationAsRead(
            int id)
        {
            int userId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value
            );

            var notification =
                await _context.Notifications
                    .FirstOrDefaultAsync(
                        n =>
                            n.NotificationId == id &&
                            n.UserId == userId
                    );

            if (notification != null)
            {
                notification.IsRead = true;

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(
                "Notifications"
            );
        }


        // ===================== ADMIN USER MANAGEMENT =====================

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ManageUsers(
            string? search)
        {
            var query = _context.Users
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(
                    u =>
                        (
                            u.FirstName +
                            " " +
                            u.LastName
                        ).Contains(search)
                        ||
                        u.Email.Contains(search)
                );
            }

            var users = await query
                .OrderBy(u => u.Role)
                .ThenBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .ToListAsync();

            ViewBag.Search = search;

            return View(users);
        }


        // ===================== ACTIVATE USER =====================

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivateUser(
            int id)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(
                    u => u.UserId == id
                );

            if (user == null)
            {
                TempData["UserManagementError"] =
                    "User not found.";

                return RedirectToAction(
                    "ManageUsers"
                );
            }

            user.IsActive = true;

            await _context.SaveChangesAsync();

            TempData["UserManagementMessage"] =
                $"{user.FirstName} {user.LastName}'s account has been activated.";

            return RedirectToAction(
                "ManageUsers"
            );
        }


        // ===================== DEACTIVATE USER =====================

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateUser(
            int id)
        {
            int currentUserId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value
            );

            if (id == currentUserId)
            {
                TempData["UserManagementError"] =
                    "You cannot deactivate your own administrator account.";

                return RedirectToAction(
                    "ManageUsers"
                );
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(
                    u => u.UserId == id
                );

            if (user == null)
            {
                TempData["UserManagementError"] =
                    "User not found.";

                return RedirectToAction(
                    "ManageUsers"
                );
            }

            user.IsActive = false;

            await _context.SaveChangesAsync();

            TempData["UserManagementMessage"] =
                $"{user.FirstName} {user.LastName}'s account has been deactivated.";

            return RedirectToAction(
                "ManageUsers"
            );
        }


        // ===================== APPROVE RECRUITER =====================

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveRecruiter(
            int id)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(
                    u => u.UserId == id
                );

            if (user == null)
            {
                TempData["UserManagementError"] =
                    "User not found.";

                return RedirectToAction(
                    "ManageUsers"
                );
            }

            if (user.Role != UserRole.Recruiter)
            {
                TempData["UserManagementError"] =
                    "Only recruiter accounts can be approved.";

                return RedirectToAction(
                    "ManageUsers"
                );
            }

            user.IsApproved = true;
            user.IsActive = true;

            await _context.SaveChangesAsync();

            TempData["UserManagementMessage"] =
                $"{user.FirstName} {user.LastName}'s recruiter account has been approved.";

            return RedirectToAction(
                "ManageUsers"
            );
        }


        // ===================== ADMIN DASHBOARD =====================

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminDashboard()
        {
            var recentUsers =
                await _context.Users
                    .OrderByDescending(
                        u => u.RegisteredOn
                    )
                    .Take(5)
                    .ToListAsync();

            ViewBag.TotalUsers =
                await _context.Users.CountAsync();

            ViewBag.TotalRecruiters =
                await _context.Users.CountAsync(
                    u => u.Role == UserRole.Recruiter
                );

            ViewBag.TotalApplicants =
                await _context.Users.CountAsync(
                    u => u.Role == UserRole.Applicant
                );

            ViewBag.ActiveVacancies =
                await _context.Vacancies.CountAsync(
                    v => v.Status == VacancyStatus.Open
                );

            return View(recentUsers);
        }


        // ===================== MANAGEMENT DASHBOARD =====================

        [Authorize(Roles = "Management")]
        public async Task<IActionResult> ManagementDashboard()
        {
            ViewBag.TotalVacancies =
                await _context.Vacancies.CountAsync();

            ViewBag.OpenVacancies =
                await _context.Vacancies.CountAsync(
                    v => v.Status == VacancyStatus.Open
                );

            ViewBag.TotalApplications =
                await _context.Applications.CountAsync();

            ViewBag.AppliedCount =
                await _context.Applications.CountAsync(
                    a => a.Status == ApplicationStatus.Applied
                );

            ViewBag.UnderReviewCount =
                await _context.Applications.CountAsync(
                    a => a.Status == ApplicationStatus.UnderReview
                );

            ViewBag.ShortlistedCount =
                await _context.Applications.CountAsync(
                    a => a.Status == ApplicationStatus.Shortlisted
                );

            ViewBag.RejectedCount =
                await _context.Applications.CountAsync(
                    a => a.Status == ApplicationStatus.Rejected
                );

            ViewBag.InterviewsCount =
                await _context.Interviews.CountAsync();

            var vacancyAnalytics =
                await _context.Vacancies
                    .Include(v => v.Applications)
                    .OrderByDescending(
                        v => v.Applications.Count
                    )
                    .Take(10)
                    .Select(v => new
                    {
                        v.JobTitle,
                        v.Department,
                        v.Location,

                        ApplicationCount =
                            v.Applications.Count,

                        Status =
                            v.Status.ToString()
                    })
                    .ToListAsync();

            ViewBag.VacancyAnalytics =
                vacancyAnalytics;

            return View();
        }


        // ===================== MANAGEMENT REPORT =====================

        [Authorize(Roles = "Management")]
        public async Task<IActionResult> ManagementReport()
        {
            var report = new ManagementReportViewModel
            {
                GeneratedOn = DateTime.Now
            };

            report.TotalVacancies =
                await _context.Vacancies.CountAsync();

            report.OpenVacancies =
                await _context.Vacancies.CountAsync(
                    v => v.Status == VacancyStatus.Open
                );

            report.CancelledVacancies =
                await _context.Vacancies.CountAsync(
                    v => v.Status == VacancyStatus.Cancelled
                );

            report.TotalApplications =
                await _context.Applications.CountAsync();

            report.AppliedApplications =
                await _context.Applications.CountAsync(
                    a => a.Status == ApplicationStatus.Applied
                );

            report.UnderReviewApplications =
                await _context.Applications.CountAsync(
                    a => a.Status == ApplicationStatus.UnderReview
                );

            report.ShortlistedApplications =
                await _context.Applications.CountAsync(
                    a => a.Status == ApplicationStatus.Shortlisted
                );

            report.RejectedApplications =
                await _context.Applications.CountAsync(
                    a => a.Status == ApplicationStatus.Rejected
                );

            report.TotalInterviews =
                await _context.Interviews.CountAsync();

            report.UpcomingInterviews =
                await _context.Interviews.CountAsync(
                    i => i.InterviewDate.Date >= DateTime.Today
                );

            report.VacancyPerformance =
                await _context.Vacancies
                    .Include(v => v.Applications)
                    .OrderByDescending(
                        v => v.Applications.Count
                    )
                    .Select(v => new VacancyReportItem
                    {
                        JobTitle = v.JobTitle,
                        Department = v.Department,
                        Location = v.Location,
                        ApplicationCount =
                            v.Applications.Count,
                        Status =
                            v.Status.ToString()
                    })
                    .ToListAsync();

            return View(report);
        }


        // ===================== ERROR =====================

        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true
        )]
        public IActionResult Error()
        {
            return View(
                new ErrorViewModel
                {
                    RequestId =
                        Activity.Current?.Id ??
                        HttpContext.TraceIdentifier
                }
            );
        }
    }
}