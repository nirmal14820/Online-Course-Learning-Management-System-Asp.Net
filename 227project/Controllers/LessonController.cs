using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using _227project.Models;
using _227project.Data;

namespace _227project.Controllers
{
    [Authorize]
    public class LessonController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public LessonController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(int courseId)
        {
            var lessons = await _context.Lessons
                .Where(l => l.CourseId == courseId && l.IsActive)
                .OrderBy(l => l.Order)
                .Include(l => l.Course)
                .ToListAsync();

            ViewBag.CourseId = courseId;
            return View(lessons);
        }

        public async Task<IActionResult> Details(int id)
        {
            var lesson = await _context.Lessons
                .Include(l => l.Course)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lesson == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(user!);

            // Check if student is enrolled
            bool isEnrolled = false;
            if (userRoles.Contains("Student"))
            {
                isEnrolled = await _context.Enrollments
                    .AnyAsync(e => e.CourseId == lesson.CourseId && e.StudentId == user!.Id && e.IsActive);
            }

            ViewBag.IsEnrolled = isEnrolled;
            ViewBag.UserRoles = userRoles;

            // Check if lesson is completed by student
            bool isCompleted = false;
            if (userRoles.Contains("Student") && isEnrolled)
            {
                var enrollment = await _context.Enrollments
                    .FirstOrDefaultAsync(e => e.CourseId == lesson.CourseId && e.StudentId == user!.Id && e.IsActive);

                if (enrollment != null)
                {
                    isCompleted = await _context.LessonProgresses
                        .AnyAsync(lp => lp.LessonId == id && lp.EnrollmentId == enrollment.Id);
                }
            }

            ViewBag.IsCompleted = isCompleted;

            return View(lesson);
        }

        [Authorize(Roles = "Admin,Instructor")]
        public IActionResult Create(int courseId)
        {
            var model = new LessonViewModel
            {
                CourseId = courseId
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Instructor")]
        public async Task<IActionResult> Create(LessonViewModel model)
        {
            if (ModelState.IsValid)
            {
                var lesson = new Lesson
                {
                    Title = model.Title,
                    Content = model.Content,
                    VideoUrl = model.VideoUrl,
                    CourseId = model.CourseId,
                    Order = await _context.Lessons.Where(l => l.CourseId == model.CourseId).CountAsync() + 1,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Lessons.Add(lesson);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Lesson created successfully!";
                return RedirectToAction("Details", new { id = lesson.Id });
            }

            return View(model);
        }

        [Authorize(Roles = "Admin,Instructor")]
        public async Task<IActionResult> Edit(int id)
        {
            var lesson = await _context.Lessons.FindAsync(id);
            if (lesson == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(user!);

            // Check if user can edit this lesson
            if (!userRoles.Contains("Admin") && lesson.Course.InstructorId != user!.Id)
            {
                return Forbid();
            }

            var model = new LessonViewModel
            {
                Id = lesson.Id,
                Title = lesson.Title,
                Content = lesson.Content,
                VideoUrl = lesson.VideoUrl,
                CourseId = lesson.CourseId
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Instructor")]
        public async Task<IActionResult> Edit(LessonViewModel model)
        {
            if (ModelState.IsValid)
            {
                var lesson = await _context.Lessons.FindAsync(model.Id);
                if (lesson == null)
                {
                    return NotFound();
                }

                var user = await _userManager.GetUserAsync(User);
                var userRoles = await _userManager.GetRolesAsync(user!);

                // Check if user can edit this lesson
                if (!userRoles.Contains("Admin") && lesson.Course.InstructorId != user!.Id)
                {
                    return Forbid();
                }

                lesson.Title = model.Title;
                lesson.Content = model.Content;
                lesson.VideoUrl = model.VideoUrl;

                _context.Update(lesson);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Lesson updated successfully!";
                return RedirectToAction("Details", new { id = lesson.Id });
            }

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> MarkComplete(int id)
        {
            var lesson = await _context.Lessons.FindAsync(id);
            if (lesson == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);

            // Check if student is enrolled
            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.CourseId == lesson.CourseId && e.StudentId == user!.Id && e.IsActive);

            if (enrollment == null)
            {
                return Forbid();
            }

            // Check if already completed
            var existingProgress = await _context.LessonProgresses
                .FirstOrDefaultAsync(lp => lp.LessonId == id && lp.EnrollmentId == enrollment.Id);

            if (existingProgress == null)
            {
                var progress = new LessonProgress
                {
                    LessonId = id,
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
