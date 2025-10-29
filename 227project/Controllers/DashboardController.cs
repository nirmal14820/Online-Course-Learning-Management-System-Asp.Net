using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using _227project.Models;
using _227project.Data;

namespace _227project.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(user!);

            if (userRoles.Contains("Admin"))
            {
                return await AdminDashboard();
            }
            else if (userRoles.Contains("Instructor"))
            {
                return await InstructorDashboard();
            }
            else
            {
                return await StudentDashboard();
            }
        }

        private async Task<IActionResult> AdminDashboard()
        {
            var stats = new AdminDashboardViewModel
            {
                TotalUsers = await _context.Users.CountAsync(),
                TotalCourses = await _context.Courses.CountAsync(),
                TotalEnrollments = await _context.Enrollments.CountAsync(e => e.IsActive),
                TotalQuizzes = await _context.Quizzes.CountAsync(),
                
                RecentCourses = await _context.Courses
                    .Include(c => c.Instructor)
                    .OrderByDescending(c => c.CreatedAt)
                    .Take(5)
                    .ToListAsync(),

                PopularCourses = await _context.Courses
                    .Include(c => c.Instructor)
                    .Include(c => c.Enrollments)
                    .OrderByDescending(c => c.Enrollments.Count(e => e.IsActive))
                    .Take(5)
                    .ToListAsync(),

                RecentUsers = await _context.Users
                    .OrderByDescending(u => u.CreatedAt)
                    .Take(5)
                    .ToListAsync()
            };

            return View("AdminDashboard", stats);
        }

        private async Task<IActionResult> InstructorDashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            
            var stats = new InstructorDashboardViewModel
            {
                MyCourses = await _context.Courses
                    .Where(c => c.InstructorId == user!.Id)
                    .CountAsync(),

                TotalStudents = await _context.Enrollments
                    .Where(e => e.Course.InstructorId == user!.Id && e.IsActive)
                    .Select(e => e.StudentId)
                    .Distinct()
                    .CountAsync(),

                MyCoursesList = await _context.Courses
                    .Where(c => c.InstructorId == user!.Id)
                    .Include(c => c.Enrollments)
                    .ToListAsync()
            };

            return View("InstructorDashboard", stats);
        }

        private async Task<IActionResult> StudentDashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            
            var enrollments = await _context.Enrollments
                .Where(e => e.StudentId == user!.Id && e.IsActive)
                .Include(e => e.Course)
                .Include(e => e.LessonProgresses)
                .ToListAsync();

            var stats = new StudentDashboardViewModel
            {
                EnrolledCourses = enrollments.Count,
                CompletedLessons = enrollments.Sum(e => e.LessonProgresses.Count),
                TotalLessons = enrollments.Sum(e => e.Course.Lessons.Count),

                QuizScores = await _context.QuizAttempts
                    .Include(qa => qa.Quiz)
                    .ThenInclude(q => q.Course)
                    .Where(qa => qa.StudentId == user!.Id)
                    .OrderByDescending(qa => qa.CompletedAt)
                    .Take(10)
                    .ToListAsync(),

                Enrollments = enrollments
            };

            return View("StudentDashboard", stats);
        }

        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Progress(int courseId)
        {
            var user = await _userManager.GetUserAsync(User);
            
            var enrollment = await _context.Enrollments
                .Include(e => e.Course)
                .ThenInclude(c => c.Lessons)
                .Include(e => e.LessonProgresses)
                .FirstOrDefaultAsync(e => e.CourseId == courseId && e.StudentId == user!.Id && e.IsActive);

            if (enrollment == null)
            {
                return NotFound();
            }

            var completedLessons = enrollment.LessonProgresses.Select(lp => lp.LessonId).ToList();
            var totalLessons = enrollment.Course.Lessons.Count;
            var completedCount = completedLessons.Count;

            var progress = new CourseProgressViewModel
            {
                Course = enrollment.Course,
                TotalLessons = totalLessons,
                CompletedLessons = completedCount,
                CompletionPercentage = totalLessons > 0 ? (double)completedCount / totalLessons * 100 : 0,
                Lessons = enrollment.Course.Lessons.OrderBy(l => l.Order).ToList(),
                CompletedLessonIds = completedLessons
            };

            return View(progress);
        }

        [HttpPost]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> MarkLessonComplete(int lessonId, int courseId)
        {
            var user = await _userManager.GetUserAsync(User);
            
            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.CourseId == courseId && e.StudentId == user!.Id && e.IsActive);

            if (enrollment == null)
            {
                return NotFound();
            }

            var existingProgress = await _context.LessonProgresses
                .FirstOrDefaultAsync(lp => lp.LessonId == lessonId && lp.EnrollmentId == enrollment.Id);

            if (existingProgress == null)
            {
                var progress = new LessonProgress
                {
                    LessonId = lessonId,
                    EnrollmentId = enrollment.Id,
                    CompletedAt = DateTime.UtcNow,
                    IsCompleted = true
                };

                _context.LessonProgresses.Add(progress);
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true });
        }
    }
}
