using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace _227project.Models
{
    public class RegisterViewModel
    {
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required]
        public string UserType { get; set; } = string.Empty; // Student or Instructor
    }

    public class LoginViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }

    public class ProfileViewModel
    {
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string? ProfilePicture { get; set; }
    }

    public class CourseViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Category { get; set; } = string.Empty;

        public string? InstructorId { get; set; }
    }


    public class QuizViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Range(1, 300)]
        public int TimeLimitMinutes { get; set; } = 30;

        [Range(1, 10)]
        public int MaxAttempts { get; set; } = 3;

        public int CourseId { get; set; }
    }

    public class QuestionViewModel
    {
        [Required]
        public string QuestionText { get; set; } = string.Empty;

        [Required]
        public string OptionA { get; set; } = string.Empty;

        [Required]
        public string OptionB { get; set; } = string.Empty;

        [Required]
        public string OptionC { get; set; } = string.Empty;

        [Required]
        public string OptionD { get; set; } = string.Empty;

        [Required]
        [StringLength(1)]
        public string CorrectAnswer { get; set; } = string.Empty;

        [Range(1, 10)]
        public int Points { get; set; } = 1;

        public int QuizId { get; set; }
    }

    public class LessonViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        [StringLength(500)]
        public string? VideoUrl { get; set; }

        public int CourseId { get; set; }
    }

    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalCourses { get; set; }
        public int TotalEnrollments { get; set; }
        public int TotalQuizzes { get; set; }
        public List<Course> RecentCourses { get; set; } = new();
        public List<Course> PopularCourses { get; set; } = new();
        public List<ApplicationUser> RecentUsers { get; set; } = new();
    }

    public class InstructorDashboardViewModel
    {
        public int MyCourses { get; set; }
        public int TotalStudents { get; set; }
        public List<Course> MyCoursesList { get; set; } = new();
    }

    public class StudentDashboardViewModel
    {
        public int EnrolledCourses { get; set; }
        public int CompletedLessons { get; set; }
        public int TotalLessons { get; set; }
        public List<QuizAttempt> QuizScores { get; set; } = new();
        public List<Enrollment> Enrollments { get; set; } = new();
    }

    public class CourseProgressViewModel
    {
        public Course Course { get; set; } = null!;
        public int TotalLessons { get; set; }
        public int CompletedLessons { get; set; }
        public double CompletionPercentage { get; set; }
        public List<Lesson> Lessons { get; set; } = new();
        public List<int> CompletedLessonIds { get; set; } = new();
    }
}
