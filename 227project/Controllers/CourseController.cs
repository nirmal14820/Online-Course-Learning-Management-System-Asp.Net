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
    public class CourseController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CourseController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(user!);

            IQueryable<Course> courses;

            if (userRoles.Contains("Admin"))
            {
                courses = _context.Courses.Include(c => c.Instructor);
            }
            else if (userRoles.Contains("Instructor"))
            {
                courses = _context.Courses
                    .Where(c => c.InstructorId == user!.Id)
                    .Include(c => c.Instructor);
            }
            else // Student - show all courses so they can browse and enroll
            {
                courses = _context.Courses
                    .Include(c => c.Instructor);
            }

            return View(await courses.ToListAsync());
        }

        public async Task<IActionResult> Details(int id)
        {
            var course = await _context.Courses
                .Include(c => c.Instructor)
                .Include(c => c.Lessons)
                .Include(c => c.Quizzes)
                .Include(c => c.Materials)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null)
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
                    .AnyAsync(e => e.CourseId == id && e.StudentId == user!.Id && e.IsActive);
            }

            ViewBag.IsEnrolled = isEnrolled;
            ViewBag.UserRoles = userRoles;

            return View(course);
        }

        [Authorize(Roles = "Admin,Instructor")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Instructor")]
        public async Task<IActionResult> Create(CourseViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                var userRoles = await _userManager.GetRolesAsync(user!);

                var course = new Course
                {
                    Title = model.Title,
                    Description = model.Description,
                    Category = model.Category,
                    InstructorId = userRoles.Contains("Admin") && !string.IsNullOrEmpty(model.InstructorId) 
                        ? model.InstructorId 
                        : user!.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Courses.Add(course);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Course created successfully!";
                return RedirectToAction("Details", new { id = course.Id });
            }

            return View(model);
        }

        [Authorize(Roles = "Admin,Instructor")]
        public async Task<IActionResult> Edit(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(user!);

            // Check if user can edit this course
            if (!userRoles.Contains("Admin") && course.InstructorId != user!.Id)
            {
                return Forbid();
            }

            var model = new CourseViewModel
            {
                Id = course.Id,
                Title = course.Title,
                Description = course.Description,
                Category = course.Category,
                InstructorId = course.InstructorId
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Instructor")]
        public async Task<IActionResult> Edit(CourseViewModel model)
        {
            if (ModelState.IsValid)
            {
                var course = await _context.Courses.FindAsync(model.Id);
                if (course == null)
                {
                    return NotFound();
                }

                var user = await _userManager.GetUserAsync(User);
                var userRoles = await _userManager.GetRolesAsync(user!);

                // Check if user can edit this course
                if (!userRoles.Contains("Admin") && course.InstructorId != user!.Id)
                {
                    return Forbid();
                }

                course.Title = model.Title;
                course.Description = model.Description;
                course.Category = model.Category;
                course.UpdatedAt = DateTime.UtcNow;

                if (userRoles.Contains("Admin") && !string.IsNullOrEmpty(model.InstructorId))
                {
                    course.InstructorId = model.InstructorId;
                }

                _context.Update(course);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Course updated successfully!";
                return RedirectToAction("Details", new { id = course.Id });
            }

            return View(model);
        }

        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Enroll(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);

            // Check if already enrolled
            var existingEnrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.CourseId == id && e.StudentId == user!.Id);

            if (existingEnrollment != null)
            {
                if (existingEnrollment.IsActive)
                {
                    TempData["ErrorMessage"] = "You are already enrolled in this course.";
                }
                else
                {
                    existingEnrollment.IsActive = true;
                    existingEnrollment.EnrolledAt = DateTime.UtcNow;
                    _context.Update(existingEnrollment);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Successfully enrolled in the course!";
                }
            }
            else
            {
                var enrollment = new Enrollment
                {
                    CourseId = id,
                    StudentId = user!.Id,
                    EnrolledAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Enrollments.Add(enrollment);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Successfully enrolled in the course!";
            }

            return RedirectToAction("Details", new { id });
        }

        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Unenroll(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.CourseId == id && e.StudentId == user!.Id);

            if (enrollment != null)
            {
                enrollment.IsActive = false;
                _context.Update(enrollment);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Successfully unenrolled from the course.";
            }

            return RedirectToAction("Index");
        }
    }
}
