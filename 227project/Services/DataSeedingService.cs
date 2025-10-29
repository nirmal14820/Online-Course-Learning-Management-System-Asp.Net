using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using _227project.Models;
using _227project.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace _227project.Services
{
    public class DataSeedingService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public DataSeedingService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task SeedDataAsync()
        {
            try
            {
                // Seed Instructors
                await SeedInstructorsAsync();

                // Seed Courses
                await SeedCoursesAsync();

                // Seed Students
                await SeedStudentsAsync();

                // Save changes
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Error seeding database", ex);
            }
        }

        private async Task SeedInstructorsAsync()
        {
            if (!await _userManager.Users.AnyAsync(u => u.Email == "instructor1@lms.com"))
            {
                var instructor = new ApplicationUser
                {
                    UserName = "instructor1@lms.com",
                    Email = "instructor1@lms.com",
                    FirstName = "John",
                    LastName = "Doe",
                    EmailConfirmed = true
                };

                await _userManager.CreateAsync(instructor, "Instructor123!");
                await _userManager.AddToRoleAsync(instructor, "Instructor");
            }
        }

        private async Task SeedStudentsAsync()
        {
            if (!await _userManager.Users.AnyAsync(u => u.Email == "student1@lms.com"))
            {
                var student = new ApplicationUser
                {
                    UserName = "student1@lms.com",
                    Email = "student1@lms.com",
                    FirstName = "Jane",
                    LastName = "Smith",
                    EmailConfirmed = true
                };

                await _userManager.CreateAsync(student, "Student123!");
                await _userManager.AddToRoleAsync(student, "Student");
            }
        }

        private async Task SeedCoursesAsync()
        {
            if (!_context.Courses.Any())
            {
                var instructor = await _userManager.FindByEmailAsync("instructor1@lms.com");
                if (instructor != null)
                {
                    var course = new Course
                    {
                        Title = "Introduction to Programming",
                        Description = "Learn the basics of programming with this comprehensive course.",
                        Category = "Programming",
                        InstructorId = instructor.Id,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.Courses.Add(course);
                    await _context.SaveChangesAsync();

                    // Add some lessons
                    var lessons = new List<Lesson>
                    {
                        new Lesson
                        {
                            Title = "Getting Started",
                            Content = "Introduction to programming concepts",
                            CourseId = course.Id,
                            Order = 1
                        },
                        new Lesson
                        {
                            Title = "Variables and Data Types",
                            Content = "Understanding different types of data in programming",
                            CourseId = course.Id,
                            Order = 2
                        }
                    };

                    _context.Lessons.AddRange(lessons);
                    await _context.SaveChangesAsync();
                }
            }
        }
    }
}
