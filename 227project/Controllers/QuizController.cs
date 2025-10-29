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
    public class QuizController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public QuizController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(int courseId)
        {
            var quizzes = await _context.Quizzes
                .Where(q => q.CourseId == courseId && q.IsActive)
                .Include(q => q.Course)
                .ToListAsync();

            ViewBag.CourseId = courseId;
            return View(quizzes);
        }

        [Authorize(Roles = "Admin,Instructor")]
        public IActionResult Create(int courseId)
        {
            var model = new QuizViewModel
            {
                CourseId = courseId
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Instructor")]
        public async Task<IActionResult> Create(QuizViewModel model)
        {
            if (ModelState.IsValid)
            {
                var quiz = new Quiz
                {
                    Title = model.Title,
                    Description = model.Description,
                    TimeLimitMinutes = model.TimeLimitMinutes,
                    MaxAttempts = model.MaxAttempts,
                    CourseId = model.CourseId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Quizzes.Add(quiz);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Quiz created successfully!";
                return RedirectToAction("Details", new { id = quiz.Id });
            }

            return View(model);
        }

        public async Task<IActionResult> Details(int id)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Course)
                .Include(q => q.Questions)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quiz == null)
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
                    .AnyAsync(e => e.CourseId == quiz.CourseId && e.StudentId == user!.Id && e.IsActive);
            }

            ViewBag.IsEnrolled = isEnrolled;
            ViewBag.UserRoles = userRoles;

            return View(quiz);
        }

        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Take(int id)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Questions.OrderBy(q => q.Order))
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quiz == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);

            // Check if student is enrolled
            var isEnrolled = await _context.Enrollments
                .AnyAsync(e => e.CourseId == quiz.CourseId && e.StudentId == user!.Id && e.IsActive);

            if (!isEnrolled)
            {
                TempData["ErrorMessage"] = "You must be enrolled in this course to take the quiz.";
                return RedirectToAction("Details", new { id });
            }

            // Check attempt limit
            var attemptCount = await _context.QuizAttempts
                .CountAsync(qa => qa.QuizId == id && qa.StudentId == user!.Id);

            if (attemptCount >= quiz.MaxAttempts)
            {
                TempData["ErrorMessage"] = $"You have reached the maximum number of attempts ({quiz.MaxAttempts}) for this quiz.";
                return RedirectToAction("Details", new { id });
            }

            // Create new attempt
            var attempt = new QuizAttempt
            {
                QuizId = id,
                StudentId = user!.Id,
                StartedAt = DateTime.UtcNow,
                AttemptNumber = attemptCount + 1,
                TotalQuestions = quiz.Questions.Count
            };

            _context.QuizAttempts.Add(attempt);
            await _context.SaveChangesAsync();

            ViewBag.AttemptId = attempt.Id;
            ViewBag.TimeLimit = quiz.TimeLimitMinutes;

            return View(quiz);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> SubmitQuiz(int attemptId, IFormCollection form)
        {
            var attempt = await _context.QuizAttempts
                .Include(qa => qa.Quiz)
                .ThenInclude(q => q.Questions)
                .FirstOrDefaultAsync(qa => qa.Id == attemptId);

            if (attempt == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (attempt.StudentId != user!.Id)
            {
                return Forbid();
            }

            attempt.CompletedAt = DateTime.UtcNow;

            int correctAnswers = 0;
            int totalQuestions = attempt.Quiz.Questions.Count;

            // Extract answers from form collection
            var answers = new Dictionary<int, string>();
            foreach (var key in form.Keys)
            {
                if (key.StartsWith("answers[") && key.EndsWith("]"))
                {
                    var questionIdStr = key.Substring(8, key.Length - 9); // Remove "answers[" and "]"
                    if (int.TryParse(questionIdStr, out int questionId))
                    {
                        answers[questionId] = form[key].ToString();
                    }
                }
            }

            foreach (var question in attempt.Quiz.Questions)
            {
                var answer = new Answer
                {
                    QuestionId = question.Id,
                    QuizAttemptId = attemptId,
                    SelectedAnswer = answers.ContainsKey(question.Id) ? answers[question.Id] : "",
                    AnsweredAt = DateTime.UtcNow
                };

                // Check if answer is correct
                if (answer.SelectedAnswer == question.CorrectAnswer)
                {
                    answer.IsCorrect = true;
                    correctAnswers++;
                }

                _context.Answers.Add(answer);
            }

            attempt.CorrectAnswers = correctAnswers;
            attempt.Score = (int)Math.Round((double)correctAnswers / totalQuestions * 100);

            _context.Update(attempt);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Quiz completed! Your score: {attempt.Score}% ({correctAnswers}/{totalQuestions})";
            return RedirectToAction("Result", new { id = attemptId });
        }

        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Result(int id)
        {
            var attempt = await _context.QuizAttempts
                .Include(qa => qa.Quiz)
                .ThenInclude(q => q.Course)
                .Include(qa => qa.Quiz)
                .ThenInclude(q => q.Questions)
                .Include(qa => qa.Answers)
                .ThenInclude(a => a.Question)
                .FirstOrDefaultAsync(qa => qa.Id == id);

            if (attempt == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (attempt.StudentId != user!.Id)
            {
                return Forbid();
            }

            return View(attempt);
        }

        [Authorize(Roles = "Admin,Instructor")]
        public async Task<IActionResult> AddQuestion(int quizId)
        {
            var quiz = await _context.Quizzes.FindAsync(quizId);
            if (quiz == null)
            {
                return NotFound();
            }

            var model = new QuestionViewModel
            {
                QuizId = quizId
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Instructor")]
        public async Task<IActionResult> AddQuestion(QuestionViewModel model)
        {
            if (ModelState.IsValid)
            {
                var question = new Question
                {
                    QuestionText = model.QuestionText,
                    OptionA = model.OptionA,
                    OptionB = model.OptionB,
                    OptionC = model.OptionC,
                    OptionD = model.OptionD,
                    CorrectAnswer = model.CorrectAnswer,
                    Points = model.Points,
                    QuizId = model.QuizId,
                    Order = await _context.Questions.Where(q => q.QuizId == model.QuizId).CountAsync() + 1
                };

                _context.Questions.Add(question);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Question added successfully!";
                return RedirectToAction("Details", new { id = model.QuizId });
            }

            return View(model);
        }
    }
}
